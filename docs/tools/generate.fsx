// --------------------------------------------------------------------------------------
// Builds the documentation from `.fsx` and `.md` files in the 'docs/content' directory
// (the generated documentation is stored in the 'docs/output' directory)
// --------------------------------------------------------------------------------------

// Binaries that have XML documentation (in a corresponding generated XML file)
let referenceBinaries = [ "FSharp.Quotations.Evaluator.dll" ]
// Web site location for the generated documentation
let website = "/FSharp.Quotations.Evaluator"

let githubLink = "http://github.com/fsprojects/FSharp.Quotations.Evaluator"

// Specify more information about your project
let info =
  [ "project-name", "FSharp.Quotations.Evaluator"
    "project-author", "F# Software Foundation"
    "project-summary", "A quotations evaluator for F# based on LINQ expression tree compilation"
    "project-github", githubLink
    "project-nuget", "http://nuget.org/packages/FSharp.Quotations.Evaluator" ]

// --------------------------------------------------------------------------------------
// For typical project, no changes are needed below
// --------------------------------------------------------------------------------------

#I "../../packages/build/FSharp.Formatting/lib/net40"
#I "../../packages/build/FSharp.Compiler.Service/lib/net40"
#I "../../packages/build/FSharpVSPowerTools.Core/lib/net45"
#r "../../packages/build/FAKE/tools/FakeLib.dll"
#r "RazorEngine.dll"
#r "FSharpVSPowerTools.Core.dll"
#r "FSharp.Literate.dll"
#r "FSharp.Markdown.dll"
#r "FSharp.CodeFormat.dll"
#r "FSharp.MetadataFormat.dll"
open Fake
open System.IO
open Fake.FileHelper
open FSharp.Literate
open FSharp.MetadataFormat

// When called from 'build.fsx', use the public project URL as <root>
// otherwise, use the current 'output' directory.
#if RELEASE
let root = website
#else
let root = "file://" + (__SOURCE_DIRECTORY__ @@ "../output")
#endif

// Paths with template/source/output locations
let bin        = __SOURCE_DIRECTORY__ @@ "../../bin"
let content    = __SOURCE_DIRECTORY__ @@ "../content"
let output     = __SOURCE_DIRECTORY__ @@ "../output"
let files      = __SOURCE_DIRECTORY__ @@ "../files"
let templates  = __SOURCE_DIRECTORY__ @@ "templates"
let formatting = __SOURCE_DIRECTORY__ @@ "../../packages/build/FSharp.Formatting/"
let docTemplate = formatting @@ "templates/docpage.cshtml"

// Where to look for *.csproj templates (in this order)
let layoutRoots =
  [ templates; formatting @@ "templates"
    formatting @@ "templates/reference" ]

// Copy static files and CSS + JS from F# Formatting
let copyFiles () =
  CopyRecursive files output true |> Log "Copying file: "
  ensureDirectory (output @@ "content")
  CopyRecursive (formatting @@ "styles") (output @@ "content") true 
    |> Log "Copying styles and scripts: "

// Build API reference from XML comments
let buildReference () =
  CleanDir (output @@ "reference")
  let binaries =
    referenceBinaries
    |> List.map (fun lib-> bin @@ lib)
  MetadataFormat.Generate
    ( binaries, output @@ "reference", layoutRoots, 
      parameters = ("root", root)::info,
      sourceRepo = githubLink @@ "tree/master",
      sourceFolder = __SOURCE_DIRECTORY__ @@ ".." @@ "..",
      publicOnly = true )

// Build documentation from `fsx` and `md` files in `docs/content`
let buildDocumentation () =
  let subdirs = Directory.EnumerateDirectories(content, "*", SearchOption.AllDirectories)
  for dir in Seq.append [content] subdirs do
    let sub = if dir.Length > content.Length then dir.Substring(content.Length + 1) else "."
    Literate.ProcessDirectory
      ( dir, docTemplate, output @@ sub, replacements = ("root", root)::info,
        layoutRoots = layoutRoots )

// Generate
copyFiles()
buildDocumentation()
buildReference()
