open System
open AlgorithmW.Syntax

let e0 = ELet ("id", EAbs ("x", EVar "x"), EVar "id")
let e1 = ELet ("id", EAbs ("x", EVar "x"), EApp (EVar "id", EVar "id"))
let e2 = ELet ("id", EAbs ("x", ELet ("y", EVar "x", EVar "y")), EApp (EVar "id", EVar "id"))
let e3 = ELet ("id", EAbs ("x", ELet ("y", EVar "x", EVar "y")), EApp (EApp (EVar "id", EVar "id"), ELit (LInt 2)))
let e4 = ELet ("id", EAbs ("x", EApp (EVar "x", EVar "x")), EVar "id") // Supposed to fail
let e5 = EAbs ("m", ELet ("y", EVar "m", ELet ("x", EApp (EVar "y", ELit (LBool true)), EVar "x")))

let test e =
    try 
        let t = typeInference Map.empty e
        printfn "%A : %A" e t
    with TypeError (m, t1, t2) -> 
        printfn "%A ! %s: %A vs. %A" e m t1 t2
    printfn ""


[<EntryPoint>]
let main argv = 
    List.map test [e0; e1; e2; e3; e4; e5] |> ignore
    Console.ReadLine() |> ignore
    0 // return an integer exit code

