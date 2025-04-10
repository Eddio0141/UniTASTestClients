use super::*;

pub fn get() -> Test {
    Test {
        name: "unity_latest",
        test,
    }
}

fn test(ctx: &mut TestCtx, mut args: TestArgs) -> Result<()> {
    let stream = &mut args.stream;

    stream.send("full_access(true)")?;
    stream.receive()?;

    ctx.run_general_tests(stream)?;

    Ok(())
}
