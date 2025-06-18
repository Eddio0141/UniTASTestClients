pub const OLD_INPUT_SYSTEM: &str = include_str!("movies/old_input_system.lua");

#[macro_export]
macro_rules! movie_name_from_const {
    ($t: path) => {{
        let t = stringify!($t);
        &t.split("::").last().unwrap().to_lowercase()
    }};
}
