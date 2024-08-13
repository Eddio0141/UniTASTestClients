{
  rustPlatform,
  pkg-config,
  openssl,
}:

rustPlatform.buildRustPackage {
  pname = "test-runner";
  version = "0.1.0";

  src = ../test-runner;
  cargoLock = {
    lockFile = ../test-runner/Cargo.lock;
  };

  nativeBuildInputs = [ pkg-config ];
  buildInputs = [ openssl ];
}
