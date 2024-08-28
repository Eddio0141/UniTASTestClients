use crate::symbols;

#[must_use]
/// Checks if `left` == `right`
/// ## Returns
/// True if equals
pub fn assert_eq<T, F>(test_name: &str, left: T, right: T, message: F) -> bool
where
    F: FnOnce() -> String,
    T: std::cmp::PartialEq,
{
    let success = left == right;
    let err = if success { None } else { Some(message()) };

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
