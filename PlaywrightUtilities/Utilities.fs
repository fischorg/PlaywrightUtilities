namespace PlaywrightUtilities

open Microsoft.Playwright
open System.Threading.Tasks
open System.Linq

[<AutoOpen>]
module Utilities =
    let mutable browser:IBrowserContext option = None

    let getBrowser (kind: Browser) (lo: BrowserTypeLaunchOptions) (pw: IPlaywright) =
        let q  =()
        task {
            //pw.Devices.[0].ScreenSize.Width <- 800
            pwout $"Browsing with {kind.ToString()}"

            return!
                match kind with
                | Firefox -> pw.Firefox.LaunchAsync(lo)
                | Webkit -> pw.Webkit.LaunchAsync(lo)
                | Chromium
                | _ ->
                    //failwith "asd"
                    pw.Chromium.LaunchAsync(lo)
        }

    let rec getPage (url: string) (pgto: PageGotoOptions) (getBrowser: Task<IBrowser>) =
        let fn () =
            task {
                pwout $"Navigating to url: {url}"
                let! b = getBrowser
                //b.Contexts |> Seq.iter (fun c -> c.SetDefaultTimeout 10f)
                let context = b.NewContextAsync() |> runAsyncT
                browser <- context |> Some
                let! page = context.NewPageAsync()
                let! x = page.GotoAsync(url, pgto)
                if x.Ok |> not then failwith $"Failed to navigate to url: {url}"
                return page
            }
            |> Async.AwaitTask
            |> Async.RunSynchronously
        TryRepeatedly 10 1000 fn
        //with _ -> getPage url pgto getBrowser

    let step pageTask (page: IPage) =
        //iStep <- iStep + 1
        //let stepNumber = iStep |> ToString |> SP.PadLeftSpecific '0' 2
        //let testName = testName
        //let dir = @"c:\temp\x\"
        let go() =
            //try
                task
                    {
                        //do! page.screenshotToPath (dir +/+ ([dir; testName; stepNumber; stepName; "Start.jpg"] |> SP.JoinUnderscores))
                        do! pageTask page
                        //do! page.screenshotToPath (dir +/+ ([dir; testName; stepNumber; stepName; "End.jpg"] |> SP.JoinUnderscores))
                    }
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> ignore
            //with ex -> go()
        go()
        page

    let tryStep pageTask (page: Result<IPage, 'err>) : Result<IPage, 'err> =
        //iStep <- iStep + 1
        //let stepNumber = iStep |> ToString |> SP.PadLeftSpecific '0' 2
        //let testName = testName
        //let dir = @"c:\temp\x\"
        page
        |> function
        | Ok page ->
            let go() =
                //try
                    task
                        {
                            //do! page.screenshotToPath (dir +/+ ([dir; testName; stepNumber; stepName; "Start.jpg"] |> SP.JoinUnderscores))
                            return! pageTask page
                            //do! page.screenshotToPath (dir +/+ ([dir; testName; stepNumber; stepName; "End.jpg"] |> SP.JoinUnderscores))
                        }
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                    //|> ignore
                //with ex -> go()
            go()
            |> function
            | Ok() -> Ok page
            | Error x -> Error x

        | Error x -> Error x

    let InstallPlaywright() =
        let retCode = Microsoft.Playwright.Program.Main([|"install"|])
        if retCode = 0 then
            Ok()
        else
            Error "Failed to install playwright"