#[macro_export]
macro_rules! assert_eq {
    ($test_name: expr, $expected: expr, $actual: expr, $fail_msg: expr, $res: ident) => {
        let success = $expected == $actual;
        let err = if success {
            None
        } else {
            $res = false;
            Some(format!(
                "{}, expected: `{:?}`, actual: `{:?}`",
                $fail_msg, $expected, $actual
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
