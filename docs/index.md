# FsHcl

A small typed HCL generation DSL for F#.

FsHcl lets you build [HCL](https://github.com/hashicorp/hcl) documents in F# using computation expressions and typed values.
It ships as a `netstandard2.0` library so it works with .NET Framework, .NET Core, and .NET 5+.

## Installation

```bash
dotnet add package FsHcl
```

## Quick Start

```fsharp
open FsHcl
open FsHcl.Hcl.Render
open FsHcl.Hcl.Syntax
open FsHcl.Hcl.Values
open FsHcl.TerraformHcl

hcl {
    resource "aws_s3_bucket" "example" {
        attr "bucket" (str "my-bucket")

        attr "tags" (
            obj {
                stringField "Environment" "production"
                stringField "ManagedBy" "terraform"
            }
        )
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

## Modules

| Module | Description |
|--------|-------------|
| [`FsHcl.Hcl.Values`](reference/FsHcl.Hcl.Values.html) | Typed values and CLR/F# value conversion |
| [`FsHcl.Hcl.Syntax`](reference/FsHcl.Hcl.Syntax.html) | Block, attribute, list, and document builders |
| [`FsHcl.Hcl.Render`](reference/FsHcl.Hcl.Render.html) | Configurable rendering |
| [`FsHcl.TerraformHcl`](reference/FsHcl.TerraformHcl.html) | Terraform-specific syntax helpers |

Read the [Usage Guide](guide.html) for detailed explanations and examples of every feature.

### Recommended Pattern

FsHcl is designed as a set of generic building blocks.
The recommended workflow is to define **project-specific helper functions** that wrap `attr`, `str`, `raw`, etc., so your HCL reads declaratively:

```fsharp
// Define helpers once
let name value = attr "name" (str value)
let role value = attr "role" (raw value)

// Use them declaratively
hcl {
    resource "aws_iam_role" "deploy" {
        name "github-actions-deploy"
        role "data.aws_iam_policy_document.assume.json"
    }
}
|> document
```

See the [Building Project-Specific Helpers](guide.html#Building-Project-Specific-Helpers) section in the guide and the [examples/HelloLambda](https://github.com/ryushiaok/FsHcl/tree/main/examples/HelloLambda) example for this pattern in practice.
