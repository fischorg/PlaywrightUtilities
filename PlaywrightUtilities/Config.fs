namespace PlaywrightUtilities

open Microsoft.Playwright
open System.Threading.Tasks
open System

[<AutoOpen>]
module Config =
    let mutable FailOnFirstError = false
    let mutable pwout : string -> unit =
        fun (x: string) ->
            Console.WriteLine x

    let TryRepeatedly (attempts: int) (sleepMS: int) fn =
        let rec _fn attempt =
            try
                fn() |> Ok
            with ex ->
                ex.Message |> pwout
                ex.StackTrace |> pwout

                if attempt < attempts then
                    System.Threading.Thread.Sleep sleepMS
                    if attempt > 2 then
                        $"Attempt #%i{attempt}: %s{ex.Message}"
                        |> pwout
                    _fn <| attempt + 1
                else
                    ex |> Error
        _fn 0