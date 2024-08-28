use crate::symbols;

#[must_use]
/// Checks if `left` == `right`
/// ## Returns
/// True if equals
pub fn assert_eq<T, F>(test_name: &str, left: T, right: T, message: F) -> bool
where
    F: FnOnce() -> String,
    T: std::cmp::PartialEq + std::fmt::Debug,
{
    let success = left == right;
    let err = if success {
        None
    } else {
        Some(format!(
            "{}, left: `{left:?}`, right: `{right:?}`",
            message()
        ))
    };

    print_test_result(test_name, success, err);

    success
}

fn print_test_result(name: &str, success: bool, error: Option<String>) {
    let symbol = if success {
        symbols::SUCCESS
    } else {
        symbols::ERROR
    };

    println!("{symbol} {name}");

    if let Some(error) = error {
        println!("  Error: {error}");
    }
}
