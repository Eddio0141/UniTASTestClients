#![allow(non_upper_case_globals)]

pub const OLD_INPUT_SYSTEM__2022_3__6000_0_44F1: &str =
    include_str!("movies/old_input_system__2022_3__6000_0_44f1.lua");

#[macro_export]
macro_rules! movie_name_from_const {
    ($t: path) => {{
        let t = stringify!($t);
        &t.split("::").last().unwrap().to_lowercase()
    }};
}
