use crate::{movie_name_from_const, movies};

use super::*;

pub fn get() -> Test {
    Test {
        name: "unity_latest",
        test,
    }
}

fn test(ctx: &mut TestCtx, mut args: TestArgs) -> Result<()> {
    let stream = &mut args.stream;

    ctx.run_init_and_general_tests(stream)?;
    ctx.run_movie_test(
        stream,
        movies::OLD_INPUT_SYSTEM__2022_3__6000_0_44F1,
        movie_name_from_const!(movies::OLD_INPUT_SYSTEM__2022_3__6000_0_44F1),
        args.game_dir,
    )?;

    Ok(())
}
