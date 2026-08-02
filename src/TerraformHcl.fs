namespace FsHcl

/// Terraform syntax helpers layered on the generic HCL DSL.
module TerraformHcl =
    open FsHcl.Hcl

    /// Creates a Terraform `terraform` block.
    let terraform = block "terraform" []

    /// Creates a Terraform `provider` block.
    let provider name = block "provider" [ name ]

    /// Creates a Terraform `resource` block.
    let resource typeName name =
        block "resource" [ typeName; name ]

    /// Creates a Terraform `data` block.
    let data sourceName name =
        block "data" [ sourceName; name ]

    /// Creates a Terraform `variable` block.
    let variable name = block "variable" [ name ]

    /// Creates a Terraform `output` block.
    let output name = block "output" [ name ]

    /// Creates a Terraform `locals` block.
    let locals = block "locals" []

    /// Creates a Terraform `module` block.
    let module_ name = block "module" [ name ]

    /// Creates a Terraform `import` block.
    let import_ = block "import" []

    /// Creates a Terraform `moved` block.
    let moved_ = block "moved" []

    /// Alias for `moved_`, useful when callers use the shorter term "move".
    let move_ = moved_

    /// Creates a Terraform `removed` block.
    let removed_ = block "removed" []

    /// Creates a Terraform `check` block.
    let check name = block "check" [ name ]

    /// Creates a Terraform `to` expression attribute, used by `import` and `moved`.
    let to_ value = attr "to" (raw value)

    /// Creates a Terraform `from` expression attribute, used by `moved` and `removed`.
    let from_ value = attr "from" (raw value)

    /// Creates a Terraform import `id` attribute.
    let id value = attr "id" (str value)

    // ---------------------------------------------------------------
    // Dynamic block
    // ---------------------------------------------------------------

    /// Creates a Terraform `dynamic` block.  The computation-expression
    /// body becomes the inner `content` block automatically.
    let dynamic_ name forEach =
        ContainerBuilder(fun body ->
            Block(
                "dynamic",
                [ name ],
                [ Attribute("for_each", Raw forEach)
                  Block("content", [], body) ]
            ))

    /// Creates a Terraform `dynamic` block with a custom `iterator` name.
    let dynamicWithIterator name forEach iterator =
        ContainerBuilder(fun body ->
            Block(
                "dynamic",
                [ name ],
                [ Attribute("for_each", Raw forEach)
                  Attribute("iterator", Raw iterator)
                  Block("content", [], body) ]
            ))

    // ---------------------------------------------------------------
    // Meta-arguments (resource / data / module)
    // ---------------------------------------------------------------

    /// Creates a `count` meta-argument.
    let count value = attr "count" value

    /// Creates a `for_each` meta-argument.
    let for_each value = attr "for_each" value

    /// Creates a `depends_on` meta-argument from a list of references.
    let depends_on refs =
        attr "depends_on" (Value.List(refs |> List.map raw))

    /// Creates a `provider` meta-argument reference (e.g. `provider = aws.west`).
    let provider_ ref = attr "provider" (raw ref)

    /// Creates a `provisioner` block (e.g. `provisioner "local-exec" { ... }`).
    let provisioner typeName =
        block "provisioner" [ typeName ]

    /// Creates a `connection` block inside a resource or provisioner.
    let connection = block "connection" []

    // ---------------------------------------------------------------
    // lifecycle block and its arguments
    // ---------------------------------------------------------------

    /// Creates a `lifecycle` block.
    let lifecycle = block "lifecycle" []

    /// `create_before_destroy = true/false` inside a `lifecycle` block.
    let create_before_destroy value =
        attr "create_before_destroy" (bool value)

    /// `prevent_destroy = true/false` inside a `lifecycle` block.
    let prevent_destroy value =
        attr "prevent_destroy" (bool value)

    /// `ignore_changes = [ref, ...]` inside a `lifecycle` block.
    let ignore_changes refs =
        attr "ignore_changes" (Value.List(refs |> List.map raw))

    /// `ignore_changes = all` inside a `lifecycle` block.
    let ignore_changes_all = attr "ignore_changes" (raw "all")

    /// `replace_triggered_by = [ref, ...]` inside a `lifecycle` block.
    let replace_triggered_by refs =
        attr "replace_triggered_by" (Value.List(refs |> List.map raw))

    /// Creates a `precondition` block (used in `lifecycle`, `output`, etc.).
    let precondition = block "precondition" []

    /// Creates a `postcondition` block (used in `lifecycle`).
    let postcondition = block "postcondition" []

    // ---------------------------------------------------------------
    // terraform block sub-blocks
    // ---------------------------------------------------------------

    /// Creates a `required_providers` block inside a `terraform` block.
    let required_providers = block "required_providers" []

    /// Creates a `required_version` attribute inside a `terraform` block.
    let required_version value = attr "required_version" (str value)

    /// Creates a `backend` block inside a `terraform` block (e.g. `backend "s3" { ... }`).
    let backend name = block "backend" [ name ]

    /// Creates a `cloud` block inside a `terraform` block.
    let cloud = block "cloud" []

    // ---------------------------------------------------------------
    // variable / output common arguments
    // ---------------------------------------------------------------

    /// Creates a `type` attribute (e.g. `type = string`).  Pass the HCL
    /// type expression as a raw string.
    let type_ value = attr "type" (raw value)

    /// Creates a `default` attribute for a variable block.
    let default_ value = attr "default" value

    /// Creates a `description` string attribute.
    let description value = attr "description" (str value)

    /// Creates a `sensitive` boolean attribute.
    let sensitive value = attr "sensitive" (bool value)

    /// Creates a `nullable` boolean attribute.
    let nullable value = attr "nullable" (bool value)

    /// Creates a `validation` block inside a variable block.
    let validation = block "validation" []

    /// Creates a `value` attribute for an output block.
    let value_ value = attr "value" value

    /// Creates a `condition` attribute (used in `validation`, `precondition`, etc.).
    let condition_ value = attr "condition" (raw value)

    /// Creates an `error_message` string attribute.
    let error_message value = attr "error_message" (str value)
