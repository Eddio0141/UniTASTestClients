#[macro_export]
macro_rules! assert_eq {
    ($test_name: expr, $left: expr, $right: expr, $fail_msg: expr, $res: ident) => {
        let success = $left == $right;
        let err = if success {
            None
        } else {
            $res = false;
            Some(format!(
                "{}, left: `{:?}`, right: `{:?}`",
                $fail_msg, $left, $right
            ))
        };

        let symbol = if success {
            $crate::symbols::SUCCESS
        } else {
            $crate::symbols::ERROR
        };

        println!("{symbol} {}", $test_name);

        if let Some(error) = err {
            println!("  Error: {error}");
        }
    };
}
