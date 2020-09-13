﻿module Check

open System
open System.Diagnostics

let info fmt =
    let sb = System.Text.StringBuilder()
    Printf.kbprintf (fun () -> sb.ToString() |> Information) sb fmt

let label s = Label s

let message format =
    Printf.kprintf (fun msg ->
        function
        | Failure f -> Failure(Message msg + ": " + f)
        | Faster (_,s) -> Faster(msg,s)
        | r -> r
    ) format

let isTrue (actual:bool) =
    if actual then Success
    else Failure(Normal "actual is false.")

let isFalse (actual:bool) =
    if actual then Failure(Normal "actual is true.")
    else Success

let lessThan (actual:'a) (expected:'a) =
    if actual < expected then Success
    else
        let a = (sprintf "%A" actual).Replace("\n","")
        let e = (sprintf "%A" expected).Replace("\n","")
        Failure(Normal "actual is not less than expected.\n     actual: " + Numeric a + "\n   expected: " + Numeric e)

let rec private equalInner (expected:obj) (actual:obj) =
    match actual, expected with
    | (:? (float[]) as a), (:? (float[]) as e) ->
        if a.Length = e.Length then
            Array.fold2 (fun (i,s) a e ->
                match s with
                | Success ->
                    i+1, equalInner e a
                | f -> i,f
            ) (-1,Success) e a
            |> function | i, Failure t -> Failure(Normal "Index: " + Numeric i + ". " + t)
                        | _, r -> r
        else
            Failure(Normal "Length differs. actual: " + Numeric a + " expected: " + Numeric e)
    | (:? (float32[]) as a), (:? (float32[]) as e) ->
        if a.Length = e.Length then
            Array.fold2 (fun (i,s) a e ->
                match s with
                | Success ->
                    i+1, equalInner e a
                | f -> i,f
            ) (-1,Success) e a
            |> function | i, Failure t -> Failure(Normal "Index: " + Numeric i + ". " + t)
                        | _, r -> r
        else
            Failure(Normal "Length differs. actual: " + Numeric a + " expected: " + Numeric e)
    | (:? float as a), (:? float as e) ->
        if a=e || Double.IsNaN a && Double.IsNaN e then Success
        else
            let a = (sprintf "%A" actual).Replace("\n","")
            let e = (sprintf "%A" expected).Replace("\n","")
            Failure(Normal "actual is not equal to expected.\n     actual: " + Numeric a + "\n   expected: " + Numeric e)
    | (:? float32 as a), (:? float32 as e) ->
        if a=e || Single.IsNaN a && Single.IsNaN e then Success
        else
            let a = (sprintf "%A" actual).Replace("\n","")
            let e = (sprintf "%A" expected).Replace("\n","")
            Failure(Normal "actual is not equal to expected.\n     actual: " + Numeric a + "\n   expected: " + Numeric e)
    | a, e ->
        if a=e then Success
        else
            let a = (sprintf "%A" actual).Replace("\n","")
            let e = (sprintf "%A" expected).Replace("\n","")
            Failure(Normal "actual is not equal to expected.\n     actual: " + Numeric a + "\n   expected: " + Numeric e)

let equal (expected:'a) (actual:'a) =
    equalInner (box expected) (box actual)

let between (actual:'a) (startInclusive:'a) (endInclusive:'a) =
    if actual < startInclusive then
        Failure(Normal "actual (" + Numeric actual + ") is less than start (" + Numeric startInclusive + ").")
    elif actual > endInclusive then
        Failure(Normal "actual (" + Numeric actual + ") is greater than end (" + Numeric endInclusive + ").")
    else Success

let close accuracy (expected:'a) (actual:'a) =
    match box expected, box actual with
    | (:? float as e), (:? float as a) ->
        if Accuracy.areClose accuracy e a then Success
        else
            Failure(Normal "Expected difference to be less than "
            + Numeric(Accuracy.areCloseRhs accuracy a e)
            + Normal ", but was " + Numeric(Accuracy.areCloseLhs a e)
            + Normal ". actual=" + Numeric a + Normal " expected=" + Numeric e
            )
    | (:? (float[]) as e), (:? (float[]) as a) ->
        Array.fold2 (fun (i,s) e a ->
            match s with
            | Success ->
                if Accuracy.areClose accuracy e a then i+1,Success
                else
                    i,Failure(
                      Normal "Index: " + Numeric i + ". "
                    + "Expected difference to be less than "
                    + Numeric(Accuracy.areCloseRhs accuracy e a)
                    + ", but was " + Numeric(Accuracy.areCloseLhs a e)
                    + ". actual=" + Numeric a + " expected=" + Numeric e
                    )
            | f -> i,f
        ) (0,Success) e a |> snd
    | _ -> failwithf "Unknown type %s" typeof<'a>.Name

let faster (expected:unit->'a) (actual:unit->'a) =
    let t1 = Stopwatch.GetTimestamp()
    let aa,ta,ae,te =
        if t1 &&& 1L = 1L then
            let aa = actual()
            let t1 = Stopwatch.GetTimestamp() - t1
            let t2 = Stopwatch.GetTimestamp()
            let ae = expected()
            let t2 = Stopwatch.GetTimestamp() - t2
            aa,t1,ae,t2
        else
            let ae = expected()
            let t1 = Stopwatch.GetTimestamp() - t1
            let t2 = Stopwatch.GetTimestamp()
            let aa = actual()
            let t2 = Stopwatch.GetTimestamp() - t2
            aa,t2,ae,t1
    match equal aa ae with
    | Success -> Faster("",if te>ta then float(te-ta)/float te else float(te-ta)/float ta)
    | fail -> fail

/// Chi-squared test to 6 standard deviations.
let chiSquared (expected:int[]) (actual:int[]) =
    if actual.Length <> expected.Length then
        Failure(Normal "actual and expected need to be the same length.")
    elif Array.exists (fun i -> i<=5) expected then
        Failure(Normal "expected frequency for all buckets needs to be above 5.")
    else
        let chi = Array.fold2 (fun s a e ->
            let d = float(a-e)
            s+d*d/float e) 0.0 actual expected
        let mean = float(expected.Length - 1)
        let sdev = sqrt(2.0 * mean)
        let SDs = (chi - mean) / sdev
        if abs SDs > 6.0 then
            Failure(Normal "chi-squared standard deviation = " + Numeric(SDs.ToString("0.0")))
        else Success
