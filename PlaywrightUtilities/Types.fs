namespace PlaywrightUtilities
open Microsoft.Playwright
open System.Threading.Tasks
open System

[<AutoOpen>]
module Types =
    type Browser =
        | Chromium
        | Chrome
        | Edge
        | Firefox
        | Webkit
    type TruelyFatalError(msg: string, ex: Exception) =
        inherit Exception(msg, ex)
    let runAsyncU (x: Task) = x |> Async.AwaitTask |> Async.RunSynchronously
        //try
        //    x |> Async.AwaitTask |> Async.RunSynchronously
        //with ex ->
        //    match ex with
        //    | :? AggregateException as ex ->
        //        ex.InnerExceptions
        //        |> Seq.toList
        //        |> function
        //        | [] ->
        //        |> Seq.iter
        //            (function
        //                | :? PlaywrightException as ex when ex.Message.Contains "as" ->
        //                    new TruelyFatalError($"This should die: %s{ex.Message}", ex)
        //                    |> fun x -> x :> exn
        //                | x -> x :> exn
        //            )
        //        ()
        //    | _ -> ()
        //    raise ex
    let runAsyncT (x: Task<'T>) =
        try
            x |> Async.AwaitTask |> Async.RunSynchronously
        with ex ->
            match ex with
            | :? AggregateException  as ex ->
                ex.InnerExceptions
                |> Seq.map (fun x -> x.Message)
                |> Seq.iter pwout
            | ex -> pwout ex.Message
            raise ex
    let lito =
        let x = LocatorInnerTextOptions()
        x.Timeout <- 1000f
        x
    let lat =
        let x = LocatorGetAttributeOptions()
        x.Timeout <- 1000f
        x
    let lwfo =
        let x = LocatorWaitForOptions()
        x.Timeout <- 60000f
        x

    type NonFatalException() =
        inherit Exception()

    type Microsoft.Playwright.ILocator with

        member this.waitForLocator = this.WaitForAsync(lwfo) |> runAsyncU

        member this.nest (selector: string) = this.Locator(selector)

        member private this.textBase demandExists o =
            try
                this.InnerTextAsync(lito) |> runAsyncT
            with ex ->
                if demandExists then raise ex
                else raise (new NonFatalException())

        member this.text = this.textBase false lito

        member private this.tryGetTextBase demandExists (selector: string) : Result<string, string> =
            try this.Locator(selector).textBase demandExists lito |> Ok
            with _ -> Error $"Failed to get text for selector: '{selector}'"

        member private this.getTextOrEmptyStringRepeatedly demandExists maxRetries (selector: string) : Result<string, string> =
            let rec tryRepeatedly cnt =
                this.tryGetTextBase demandExists selector
                |> function
                | Ok x -> x |> Ok
                | Error _ ->
                    if cnt > maxRetries then
                        if maxRetries = 10 then ()
                        let msg = $"[getNestedTextOrEmptyStringRepeatedly] Too many attempts to find selector '{selector}'. MaxRetries: {maxRetries}"
                        if demandExists then
                            if Config.FailOnFirstError then
                                failwith msg
                        msg |> Error
                    else
                        let newCnt = cnt + 1
                        let msg = $"[getNestedTextOrEmptyStringRepeatedly] Searching for selector '{selector}'. Attempt: {newCnt}."
                        if demandExists then
                            pwout msg
                        tryRepeatedly newCnt
            tryRepeatedly 0

        member this.tryGetText (selector: string) = this.getTextOrEmptyStringRepeatedly true 10 selector

        member this.getTextOrEmptyStringFast (selector: string) : string =
            this.getTextOrEmptyStringRepeatedly false 2 selector
            |> function
            | Ok x -> x
            | Error x ->
                //log error x
                ""

        member this.countChildren (selector: string) = this.Locator(selector).CountAsync() |> runAsyncT

        member this.getAllChildren (selector: string) = this.Locator(selector).AllAsync() |> runAsyncT |> Seq.toList

        member private this.getAttributeValue (attr: string) = this.GetAttributeAsync(attr, lat) |> runAsyncT

        member private this.tryGetAttributeValueBase (attr: string) (selector: string) : Result<string, string> =
            try
                selector
                |> function
                | "" -> this.getAttributeValue attr
                | selector ->
                    let l = this.Locator(selector)
                    l.getAttributeValue attr
                |> Ok
            with _ -> Error $"Failed to get text for selector '{selector}' with attribute '{attr}'"

        member private this.getAttributeValueOrEmptyStringRepeatedly demandExists maxRetries (attr: string) (selector: string) : Result<string, string> =
            let rec tryRepeatedly cnt =
                this.tryGetAttributeValueBase attr selector
                |> function
                | Ok x -> x |> Ok
                | Error _ ->
                    if cnt > maxRetries then
                        let msg = $"[getAttributeValueOrEmptyStringRepeatedly] Too many attempts to locate selector '{selector}' with attr '{attr}'. MaxRetries: {maxRetries}"
                        if Config.FailOnFirstError then
                            failwith msg
                        msg |> Error
                    else
                        let newCnt = cnt + 1
                        let msg = $"[getAttributeValueOrEmptyStringRepeatedly] Searching for selector '{selector}' with attr '{attr}'. Attempt {newCnt}"
                        if demandExists then
                            pwout msg
                        tryRepeatedly newCnt
            tryRepeatedly 0

        member this.tryGetAttributeValue (attr: string) (selector: string) = this.getAttributeValueOrEmptyStringRepeatedly true 10 attr selector

        member this.tryGetAttributeValueSelf (attr: string) = this.tryGetAttributeValue attr ""

        member this.getAttributeValueOrEmptyStringFast (attr: string) (selector: string) : string =
            this.getAttributeValueOrEmptyStringRepeatedly false 2 attr selector
            |> function
            | Ok x -> x
            | Error x ->
                //log error x
                ""

        member this.isVisible = this.IsVisibleAsync() |> runAsyncT

        //member this.tryGetAttrValueIfVisible (attr: string) (selector: string) =
        //    try
        //        let l = this.Locator(selector)
        //        if l.IsVisibleAsync() |> runAsyncT then
        //            l.GetAttributeAsync(attr, lat) |> runAsyncT |> Ok
        //        else Error "Selector does not exist: {selector}"
        //    with _ -> Error $"Failed to get attribute from selector: {selector}"

        //member this.getNestedAttrValueOrEmptyStringIfVisible (attr: string) (selector: string) =
        //    this.tryGetAttrValueIfVisible attr selector
        //    |> function
        //    | Ok x -> x
        //    | Error _ ->
        //        out $"Didn't find selector: {selector}"
        //        ""

        //member private this.tryGetNestedTextIfVisible (selector: string) =
        //    try
        //        let l = this.Locator(selector)
        //        if l.IsVisibleAsync() |> runAsyncT then
        //            l.text |> Ok
        //        else Error $"Selector does not exist: {selector}"
        //    with _ -> Error $"Failed to get text for selector: {selector}"

        //member private this.getNestedTextOrEmptyStringIfVisible selector =
        //    this.tryGetNestedTextIfVisible selector
        //    |> function
        //    | Ok x -> x
        //    | Error _ ->
        //        out $"Didn't find selector: {selector}"
        //        ""

    type Microsoft.Playwright.IPage with

        member this.getAllChildren selector =
            let maxCount = 10
            let rec tryRepeatedly cnt =
                let l = this.Locator(selector)
                try
                    l.AllAsync() |> runAsyncT
                with _ ->
                    if cnt > maxCount then failwith "Could not locate all items."
                    else
                        System.Threading.Thread.Sleep 1000
                        tryRepeatedly (cnt + 1)
            tryRepeatedly maxCount

        member this.goTo url =
            pwout $"Navigating to {url}"
            this.GotoAsync(url) |> runAsyncU

        member this.fill selector v = this.FillAsync(selector, v)

        member this.waitForSelector selector = this.Locator(selector).WaitForAsync() |> runAsyncU

        member this.waitForLocator (locator: ILocator) = locator.waitForLocator

        member this.waitForSelectorQuiet selector = this.waitForSelector selector |> ignore

        member this.isVisible selector = this.Locator(selector).IsVisibleAsync() |> runAsyncT

        member this.getTextOrEmptyStringFast selector =
            this.Locator(selector)

        member this.screenshotToPath path =
            let o = new PageScreenshotOptions()
            o.Path <- path

            task
                {
                    let! bytes = this.ScreenshotAsync o
                    ()
                }

        member this.fillAndKeyUp selector v =
            task
                {
                    do! this.FillAsync(selector, v)
                    do! this.DispatchEventAsync(selector, "keyup")
                }