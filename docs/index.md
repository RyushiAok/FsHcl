# FsHcl

A typed HCL generation DSL for F#.

## Installation

```bash
dotnet add package FsHcl
```

## Quick Start

```fsharp
open FsHcl.Hcl
open FsHcl.TerraformHcl

hcl {
    resource "aws_s3_bucket" "example" {
        attr "bucket" (str "my-bucket")

        attr "tags" (ofRecord {|
            Environment = "production";
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

## Project-Specific Helpers

Define thin wrapper functions to avoid repeating `attr "key" (str "value")`:

```fsharp
let name value = attr "name" (str value)
let role value = attr "role" (raw value)

hcl {
    resource "aws_iam_role" "deploy" {
        name "github-actions-deploy"
        role "data.aws_iam_policy_document.assume.json"
    }
}
|> document
```

See [examples/HelloLambda](https://github.com/ryushiaok/FsHcl/tree/main/examples/HelloLambda) for a full working example.

Read the [Usage Guide](guide.html) for all available functions.
