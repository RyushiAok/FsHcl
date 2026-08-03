# FsHcl

[![NuGet](https://img.shields.io/nuget/v/FsHcl.svg)](https://www.nuget.org/packages/FsHcl)

**FsHcl** generates HCL (HashiCorp Configuration Language) code from F#.
You write Terraform configurations with full type safety. FsHcl renders them to `.tf` files.

## Features

- Computation expression syntax for HCL blocks (`resource`, `data`, `module`, `variable`, `output`, `locals`, `provider`, `terraform`, and more)
- F# anonymous records as HCL values with `ofValue` / `ofRecord`
- Terraform-specific helpers in the `TerraformHcl` module
- Targets `netstandard2.0` (compatible with .NET 6, 8, 9, and later)

## Quick Start

```
dotnet add package FsHcl
```

```fsharp
open FsHcl.Hcl
open FsHcl.TerraformHcl

hcl {
    resource "aws_s3_bucket" "example" {
        attr "bucket" (str "my-bucket")
    }
}
|> document
```

## Documentation

Read the full documentation at <https://ryushiaok.github.io/FsHcl/>.

## Examples

See [examples/HelloLambda](examples/HelloLambda) for a complete working example.

## License

[MIT](LICENSE)
