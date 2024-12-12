$dst = ".git/hooks/post-checkout"
New-Item -ItemType HardLink -Path "../$dst" -Value ./post-checkout -Force
