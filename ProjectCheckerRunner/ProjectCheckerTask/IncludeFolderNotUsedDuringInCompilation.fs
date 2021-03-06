﻿namespace ProjectCheckerTask

open MSBuildHelper
open System.IO
open RuleBase
open ProjectTypes

type IncludeFolderNotUsedDuringInCompilation() = 
    inherit RuleBase()
    let IncludeFolderNotUsedDuringInCompilation =
        new Rule(Key = "IncludeFolderNotUsedDuringInCompilation",
                 Description = "Additional include folder used during compilation can be removed as it is not used")

    let IncludeFileNotFound =
        new Rule(Key = "IncludeFileNotFound",
                 Description = "Additional include folder used during compilation can be removed as it is not used")

    let mutable additionalIncludeDirectories : Set<string> = Set.empty
    let mutable includes : Set<string> = Set.empty

    override this.SupportsProject(path) =
        path.EndsWith(".vcxproj")

    override this.ExecuteCheckMsbuild(project, path, lines, solution, outputPath, includefolderstoignore) =

        try
            additionalIncludeDirectories <- Set.empty
            includes <- Set.empty

            for item in project.Items do
                try
                    if item.ItemType.Equals("ClCompile")  ||  item.ItemType.Equals("ClInclude")  then
                        let project = new ProjectTypes.Project()
                        project.Path <- path
                        GenerateBuildCppBuildInformation(&additionalIncludeDirectories, &includes, item, project, "", false)
                with
                | ex -> this.SaveIssue(path, 1, "Include Not Found in Disk " + item.EvaluatedInclude + " can be removed, its not used", IncludeFileNotFound)

            let VerifyPathExists(c, m) =
                try
                    File.Exists(Path.Combine(c, m))
                with
                | ex -> false
    
            let CheckFileExists(c) = 
                let found = includes |> Seq.tryFind(fun m ->  VerifyPathExists(c, m))
                match found with
                | Some value -> true
                | _ -> false

            additionalIncludeDirectories |> Seq.iter (fun c ->  
                    if not(CheckFileExists(c)) then 
                        let foundInIgnore = includefolderstoignore |> Seq.tryFind ( fun d -> c.ToLower().Contains(d.ToLower()))
                        match foundInIgnore with
                        | Some(d) -> ()
                        | _ -> 
                            if not(c.ToLower().Contains("packages")) then
                                this.SaveIssue(path, 1, "Additional Include Path " + c + " can be removed, its not used", IncludeFolderNotUsedDuringInCompilation)
                    else
                        ())

        with
        | ex -> ()

                                
    override this.GetRules =
        [IncludeFolderNotUsedDuringInCompilation;IncludeFileNotFound]