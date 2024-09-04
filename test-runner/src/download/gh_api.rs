use std::result;

use anyhow::{bail, Context, Result};
use regex::Regex;
use reqwest::{header::ACCEPT, RequestBuilder};
use serde_json::Value;

const GITHUB_API_LINK: &str = "https://api.github.com";

pub struct Artifact {
    pub link: String,
    pub dl_len: u64,
    pub name: String,
}

pub enum ArtifactFilter<'a> {
    TargetName(&'a str),
    ExcludeNamePattern(Regex),
}

pub async fn latest_artifacts<'a>(
    owner: &str,
    repo: &str,
    token: &str,
    workflow_name: &str,
    branch: &str,
    filter: Option<ArtifactFilter<'a>>,
) -> Result<Vec<Artifact>> {
    let workflow_id = get(
        owner,
        repo,
        token,
        &format!("actions/workflows/{workflow_name}"),
    )
    .await
    .with_context(|| format!("failed to get workflow by name `{workflow_name}`"))?;
    let workflow_id = workflow_id.get("id").unwrap().as_u64().unwrap();

    let get_latest_action_fail_msg =
        || format!("failed to get latest github actions run by id {workflow_id}");

    let get_run = |page: u64| async move {
        get(
            owner,
            repo,
            token,
            &format!("actions/workflows/{workflow_id}/runs?per_page=1&page={page}"),
        )
        .await
        .with_context(get_latest_action_fail_msg)
    };

    let latest_run = get_run(1).await?;

    // check if use run, return Some(id) if its valid
    let use_run = |workflow: &Value| {
        if workflow.get("status").unwrap().as_str().unwrap() == "completed"
            && workflow.get("head_branch").unwrap().as_str().unwrap() == branch
            && workflow.get("conclusion").unwrap().as_str().unwrap() == "success"
        {
            Some(workflow.get("id").unwrap().as_u64().unwrap())
        } else {
            None
        }
    };
    let run_count = latest_run.get("total_count").unwrap().as_u64().unwrap();
    let mut latest_run_id = use_run(
        latest_run
            .get("workflow_runs")
            .unwrap()
            .get(0)
            .with_context(get_latest_action_fail_msg)?,
    );

    if latest_run_id.is_none() {
        for i in 1..run_count {
            let run = get_run(i + 1).await?;
            latest_run_id = use_run(
                run.get("workflow_runs")
                    .unwrap()
                    .get(0)
                    .with_context(get_latest_action_fail_msg)?,
            );

            if latest_run_id.is_some() {
                break;
            }
        }
    }

    let Some(latest_run_id) = latest_run_id else {
        bail!("couldn't find workflow by name `{workflow_name}` and branch `{branch}`");
    };

    let artifacts = get(
        owner,
        repo,
        token,
        &format!("actions/runs/{latest_run_id}/artifacts"),
    )
    .await
    .with_context(get_latest_action_fail_msg)?;

    let urls = artifacts
        .get("artifacts")
        .unwrap()
        .as_array()
        .unwrap()
        .iter();

    let urls = if let Some(filter) = filter {
        urls.filter_map(|a| {
            let artifact_name = artifact_name(a);

            let matches = match &filter {
                ArtifactFilter::TargetName(n) => artifact_name == *n,
                ArtifactFilter::ExcludeNamePattern(pattern) => !pattern.is_match(artifact_name),
            };

            if matches {
                Some(Artifact {
                    name: artifact_name.to_owned(),
                    link: artifact_dl_link(a).to_owned(),
                    dl_len: artifact_size(a),
                })
            } else {
                None
            }
        })
        .collect()
    } else {
        urls.map(|a| Artifact {
            name: artifact_name(a).to_owned(),
            link: artifact_dl_link(a).to_owned(),
            dl_len: artifact_size(a),
        })
        .collect()
    };

    Ok(urls)
}

fn artifact_name(artifact: &Value) -> &str {
    artifact.get("name").unwrap().as_str().unwrap()
}

fn artifact_size(artifact: &Value) -> u64 {
    artifact.get("size_in_bytes").unwrap().as_u64().unwrap()
}

fn artifact_dl_link(artifact: &Value) -> &str {
    artifact
        .get("archive_download_url")
        .unwrap()
        .as_str()
        .unwrap()
}

pub async fn gh_api_client(
    url: &str,
    token: &str,
) -> result::Result<RequestBuilder, reqwest::Error> {
    let builder = reqwest::Client::builder()
        .user_agent(env!("CARGO_PKG_NAME"))
        .build()?
        .get(url)
        .header(ACCEPT, "application/vnd.github+json")
        .bearer_auth(token)
        .header("X-GitHub-Api-Version", "2022-11-28");

    Ok(builder)
}

async fn get(
    owner: &str,
    repo: &str,
    token: &str,
    path: &str,
) -> result::Result<Value, reqwest::Error> {
    gh_api_client(
        &format!("{GITHUB_API_LINK}/repos/{owner}/{repo}/{path}"),
        token,
    )
    .await?
    .send()
    .await?
    .json()
    .await
}
