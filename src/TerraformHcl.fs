namespace FsHcl

/// Terraform syntax helpers layered on the generic HCL DSL.
module TerraformHcl =
    open FsHcl.Hcl.Syntax
    open FsHcl.Hcl.Values

    /// Creates a Terraform `terraform` block.
    let terraform = block "terraform"

    /// Creates a Terraform `provider` block.
    let provider name = blockWithLabels "provider" [ name ]

    /// Creates a Terraform `resource` block.
    let resource typeName name =
        blockWithLabels "resource" [ typeName; name ]

    /// Creates a Terraform `data` block.
    let data sourceName name =
        blockWithLabels "data" [ sourceName; name ]

    /// Creates a Terraform `variable` block.
    let variable name =
        blockWithLabels "variable" [ name ]

    /// Creates a Terraform `output` block.
    let output name =
        blockWithLabels "output" [ name ]

    /// Creates a Terraform `locals` block.
    let locals = block "locals"

    /// Creates a Terraform `module` block.
    let module_ name =
        blockWithLabels "module" [ name ]

    /// Creates a Terraform `import` block.
    let import_ = block "import"

    /// Creates a Terraform `moved` block.
    let moved_ = block "moved"

    /// Alias for `moved_`, useful when callers use the shorter term "move".
    let move_ = moved_

    /// Creates a Terraform `removed` block.
    let removed_ = block "removed"

    /// Creates a Terraform `check` block.
    let check name =
        blockWithLabels "check" [ name ]

    /// Creates a Terraform `to` expression attribute, used by `import` and `moved`.
    let to_ value = attr "to" (raw value)

    /// Creates a Terraform `from` expression attribute, used by `moved` and `removed`.
    let from_ value = attr "from" (raw value)

    /// Creates a Terraform import `id` attribute.
    let id value = attr "id" (str value)
