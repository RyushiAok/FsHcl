---
title: Patterns
category: Documentation
categoryindex: 1
index: 5
---

# Patterns

This page shows recommended patterns for FsHcl projects.

## Define Project-Specific Helpers

Wrap `attr`, `str`, and `raw` in short functions. These helpers remove repetition and make the code easier to read.

```fsharp
module MyProject =
    open FsHcl.Hcl

    let name value = attr "name" (str value)
    let region value = attr "region" (str value)
    let role value = attr "role" (raw value)
    let function_name value = attr "function_name" (raw value)
```

Use these helpers in your configuration:

```fsharp
open MyProject
open FsHcl.Hcl
open FsHcl.TerraformHcl

hcl {
    resource "aws_iam_role" "deploy" {
        name "github-actions-deploy"
        role "data.aws_iam_policy_document.assume.json"
    }

    resource "aws_lambda_function_url" "api" {
        function_name "data.aws_lambda_function.api.function_name"
        attr "authorization_type" (str "NONE")
        depends_on [ "aws_lambda_function.api" ]
    }
}
|> document
// resource "aws_iam_role" "deploy" {
//   name = "github-actions-deploy"
//   role = data.aws_iam_policy_document.assume.json
// }
// resource "aws_lambda_function_url" "api" {
//   function_name      = data.aws_lambda_function.api.function_name
//   authorization_type = "NONE"
//   depends_on = [
//     aws_lambda_function.api,
//   ]
// }
```

## Choose Between str and raw

Use `str` for literal string values. Use `raw` for Terraform references and expressions.

The helper function makes this distinction explicit. Callers do not need to decide which wrapper to use.

```fsharp
let bucket value = attr "bucket" (str value)         // literal string
let role_arn value = attr "role_arn" (raw value)      // Terraform reference
```

## Wrap Repeated Nested Blocks

For nested blocks that appear in many resources, define a block wrapper. Do not hide the block body inside a function.

```fsharp
let statement = block "statement" []
```

Use the wrapper like a standard block:

```fsharp
resource "aws_iam_policy_document" "example" {
    statement {
        attr "effect" (str "Allow")
        list_ "actions" [ str "s3:GetObject" ]
        list_ "resources" [ str "*" ]
    }
}
```

## Complete Example

See [examples/HelloLambda](https://github.com/ryushiaok/FsHcl/tree/main/examples/HelloLambda) for a full project that uses these patterns.
