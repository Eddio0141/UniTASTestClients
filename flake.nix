{
  inputs = {
    fenix = {
      url = "github:nix-community/fenix";
      inputs.nixpkgs.follows = "nixpkgs";
    };
    rust-overlay = {
      url = "github:oxalica/rust-overlay";
      inputs.nixpkgs.follows = "nixpkgs";
    };
    nixpkgs.url = "github:NixOS/nixpkgs";
    flake-parts.url = "github:hercules-ci/flake-parts";
  };

  outputs =
    {
      flake-parts,
      nixpkgs,
      rust-overlay,
      ...
    }@inputs:
    flake-parts.lib.mkFlake { inherit inputs; } {
      systems = [ "x86_64-linux" ];
      perSystem =
        {
          self',
          inputs',
          system,
          pkgs,
          ...
        }:
        let
          rust-doc = pkgs.writeShellApplication {
            name = "rust-doc";
            text = ''
              xdg-open "${inputs'.fenix.packages.stable.rust-docs}/share/doc/rust/html/index.html"
            '';
          };

          rust = pkgs.rust-bin.stable.latest.default;
        in
        {
          _module.args.pkgs = import nixpkgs {
            inherit system;
            overlays = [ (import rust-overlay) ];
          };

          devShells.default = pkgs.mkShell {
            RUST_BACKTRACE = "1";
            packages = with pkgs; [
              (rust.override {
                extensions = [
                  "rust-analyzer"
                  "rust-src"
                ];
              })
              rust-doc
              openssl
              pkg-config
            ];
          };

          packages.test-runner = pkgs.callPackage (import ./nix/build-test-runner.nix) { };
          packages.default = self'.packages.test-runner;
        };
    };
}
