open System
open FsHcl.Hcl
open FsHcl.TerraformHcl

module HclHelper =

    let string key value = attr key (str value)
    let expr key value = attr key (raw value)
    let hclString key value = attr key (raw $"\"{value}\"")

    let stringItems values = values |> List.map (str >> item)

    let hclStringValue value = raw $"\"{value}\""

    let exprItems values = values |> List.map (raw >> item)

    let hclStringItems values =
        values |> List.map (hclStringValue >> item)

    let stringList name values =
        list_ name { yield! stringItems values }

    let hclStringList name values =
        list_ name { yield! hclStringItems values }

    let exprList name values = list_ name { yield! exprItems values }

    let hello_lambda_account_id value = string "hello_lambda_account_id" value
    let hello_lambda_repo value = string "hello_lambda_repo" value
    let hello_lambda_name value = string "hello_lambda_name" value
    let required_version value = string "required_version" value
    let region value = string "region" value
    let tags value = attr "tags" value
    let awsApplication value = string "awsApplication" value
    let environment value = string "Environment" value
    let managedBy value = string "ManagedBy" value
    let project value = string "Project" value
    let url value = string "url" value
    let client_id_list values = stringList "client_id_list" values
    let thumbprint_list values = stringList "thumbprint_list" values
    let effect value = string "effect" value
    let actions values = stringList "actions" values
    let resources values = stringList "resources" values
    let hclStringResources values = hclStringList "resources" values
    let name value = string "name" value
    let assume_role_policy value = expr "assume_role_policy" value
    let role value = expr "role" value
    let policy value = expr "policy" value
    let policy_arn value = string "policy_arn" value
    let function_name value = expr "function_name" value
    let authorization_type value = string "authorization_type" value
    let statement_id value = string "statement_id" value
    let action value = string "action" value
    let principal value = string "principal" value
    let function_url_auth_type value = string "function_url_auth_type" value
    let depends_on values = exprList "depends_on" values
    let template_body value = attr "template_body" value
    let value value = expr "value" value

    let required_providers body =
        block "required_providers" { yield! body }

    let providerConfig name body = object_ name { yield! body }

    let default_tags body = block "default_tags" { yield! body }

    let statement body = block "statement" { yield! body }

    let rawPrincipals principalType identifiers =
        block "principals" {
            string "type" principalType
            exprList "identifiers" identifiers
        }

    let stringPrincipals principalType identifiers =
        block "principals" {
            string "type" principalType
            stringList "identifiers" identifiers
        }

    let condition test variable values =
        block "condition" {
            string "test" test
            string "variable" variable
            stringList "values" values
        }

    let hclStringCondition test variable values =
        block "condition" {
            string "test" test
            string "variable" variable
            hclStringList "values" values
        }

open HclHelper

let mainTf =

    hcl {
        terraform {
            required_version ">= 1.15.0"

            required_providers [
                providerConfig "aws" [ string "source" "hashicorp/aws"; string "version" "~> 6.0" ]
            ]
        }

        provider "aws" {
            region "ap-northeast-1"

            default_tags [
                tags (
                    obj {
                        stringField
                            "awsApplication"
                            "arn:aws:resource-groups:ap-northeast-1:123456789012:group/project-name-workspace-name/PLACEHOLDER"

                        stringField "Environment" "workspace-name"
                        stringField "ManagedBy" "terraform"
                        stringField "Project" "project-name"
                    }
                )
            ]
        }

        resource "aws_ssm_parameter" "workspace-name_check" {
            name "/project-name/workspace-name/check"
            string "type" "String"
            string "value" "ok"
        }

        data "aws_caller_identity" "current" { }

        output "account_id" { value "data.aws_caller_identity.current.account_id" }

        output "caller_arn" { value "data.aws_caller_identity.current.arn" }
    }

let helloLambdaTf =
    hcl {
        locals {
            hello_lambda_account_id "123456789012"
            hello_lambda_repo "OWNER/hello-lambda"
            hello_lambda_name "hello-lambda"
        }

        resource "aws_iam_openid_connect_provider" "github_actions" {
            url "https://token.actions.githubusercontent.com"
            client_id_list [ "sts.amazonaws.com" ]
            thumbprint_list [ "6938fd4d98bab03faadb97b34396831e3780aea1" ]
        }

        data "aws_iam_policy_document" "hello_lambda_github_actions_assume_role" {
            statement [
                effect "Allow"
                actions [ "sts:AssumeRoleWithWebIdentity" ]

                rawPrincipals "Federated" [ "aws_iam_openid_connect_provider.github_actions.arn" ]

                condition "StringEquals" "token.actions.githubusercontent.com:aud" [ "sts.amazonaws.com" ]

                hclStringCondition "StringLike" "token.actions.githubusercontent.com:sub" [
                    "repo:${local.hello_lambda_repo}:ref:refs/heads/main"
                ]
            ]
        }

        resource "aws_iam_role" "hello_lambda_github_actions_deploy" {
            name "github-actions-hello-lambda-deploy"
            assume_role_policy "data.aws_iam_policy_document.hello_lambda_github_actions_assume_role.json"
        }

        data "aws_iam_policy_document" "hello_lambda_github_actions_deploy" {
            statement [
                effect "Allow"

                actions [ "lambda:GetFunction"; "lambda:CreateFunction"; "lambda:UpdateFunctionCode" ]

                hclStringResources [
                    "arn:aws:lambda:ap-northeast-1:${local.hello_lambda_account_id}:function:${local.hello_lambda_name}"
                ]
            ]

            statement [
                effect "Allow"
                actions [ "iam:PassRole" ]
                exprList "resources" [ "aws_iam_role.hello_lambda_execution.arn" ]
            ]
        }

        resource "aws_iam_role_policy" "hello_lambda_github_actions_deploy" {
            name "hello-lambda-deploy"
            role "aws_iam_role.hello_lambda_github_actions_deploy.id"
            policy "data.aws_iam_policy_document.hello_lambda_github_actions_deploy.json"
        }

        data "aws_iam_policy_document" "hello_lambda_execution_assume_role" {
            statement [
                effect "Allow"
                actions [ "sts:AssumeRole" ]
                stringPrincipals "Service" [ "lambda.amazonaws.com" ]
            ]
        }

        resource "aws_iam_role" "hello_lambda_execution" {
            name "hello-lambda-execution"
            assume_role_policy "data.aws_iam_policy_document.hello_lambda_execution_assume_role.json"
        }

        resource "aws_iam_role_policy_attachment" "hello_lambda_execution_basic" {
            role "aws_iam_role.hello_lambda_execution.name"
            policy_arn "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
        }

        data "aws_lambda_function" "hello_lambda" { function_name "local.hello_lambda_name" }

        resource "aws_lambda_function_url" "hello_lambda" {
            function_name "data.aws_lambda_function.hello_lambda.function_name"
            authorization_type "NONE"
        }

        resource "aws_lambda_permission" "hello_lambda_function_url_invoke_url" {
            statement_id "AllowPublicInvokeFunctionUrl"
            action "lambda:InvokeFunctionUrl"
            function_name "data.aws_lambda_function.hello_lambda.function_name"
            principal "*"
            function_url_auth_type "NONE"
            depends_on [ "aws_lambda_function_url.hello_lambda" ]
        }

        resource "aws_cloudformation_stack" "hello_lambda_function_url_invoke_function_permission" {
            name "hello-lambda-function-url-invoke-function-permission"

            template_body (
                jsonencode {|
                    AWSTemplateFormatVersion = "2010-09-09"
                    Resources = {|
                        InvokeFunctionPermission = {|
                            Type = "AWS::Lambda::Permission"
                            Properties = {|
                                Action = "lambda:InvokeFunction"
                                FunctionName = Values.expr "data.aws_lambda_function.hello_lambda.function_name"
                                Principal = "*"
                                InvokedViaFunctionUrl = true
                            |}
                        |}
                    |}
                |}
            )

            depends_on [ "aws_lambda_function_url.hello_lambda" ]
        }

        output "hello_lambda_github_actions_deploy_role_arn" { value "aws_iam_role.hello_lambda_github_actions_deploy.arn" }

        output "hello_lambda_execution_role_arn" { value "aws_iam_role.hello_lambda_execution.arn" }

        output "hello_lambda_function_url" { value "aws_lambda_function_url.hello_lambda.function_url" }
    }

[<EntryPoint>]
let main _ =
    mainTf |> document |> Console.Write
    Console.WriteLine()
    helloLambdaTf |> document |> Console.Write
    0
