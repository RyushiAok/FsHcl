---
title: FsHcl
category: Documentation
categoryindex: 1
index: 0
---

# FsHcl

FsHcl is a typed DSL that generates HCL code from F#.
You write Terraform configurations with computation expressions. FsHcl renders the output to valid `.tf` syntax.

## Installation

```bash
dotnet add package FsHcl
```

## Minimal Example

```fsharp
open FsHcl.Hcl
open FsHcl.TerraformHcl

hcl {
    resource "aws_s3_bucket" "example" {
        attr "bucket" (str "my-bucket")

        attr "tags" (ofRecord {|
            Environment = "production"
            ManagedBy = "terraform"
        |})
    }

    output "bucket_arn" {
        attr "value" (raw "aws_s3_bucket.example.arn")
    }
}
|> document
```

Output:

```hcl
resource "aws_s3_bucket" "example" {
  bucket = "my-bucket"

  tags = {
    Environment = "production"
    ManagedBy   = "terraform"
  }
}
output "bucket_arn" {
  value = aws_s3_bucket.example.arn
}
```

## Documentation

- [Getting Started](getting-started.html) — Install FsHcl and generate your first resource
- [Values](values.html) — All value types: strings, numbers, objects, lists, and CLR conversion
- [Syntax](syntax.html) — Blocks, attributes, comments, loops, and render options
- [Terraform Helpers](terraform.html) — Terraform-specific block builders and meta-arguments
- [Patterns](patterns.html) — Project-specific helper functions and best practices

## Examples

See [examples/HelloLambda](https://github.com/ryushiaok/FsHcl/tree/main/examples/HelloLambda) for a complete working example.
