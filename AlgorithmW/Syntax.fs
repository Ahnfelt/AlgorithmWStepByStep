module AlgorithmW.Syntax

type Lit =
    | LInt of int
    | LBool of bool

type Exp =
    | EVar of string
    | ELit of Lit
    | EApp of Exp * Exp
    | ELet of string * Exp * Exp

type Type =
    | TVar of string
    | TInt
    | TBool
    | TFun of Type * Type

type Scheme = Scheme of List<string> * Type

type Subst = Map<string, Type>

type Types<'a> = {
    ftv : 'a -> Set<string>;
    apply : Subst -> 'a -> 'a
}

let rec typeTypes : Types<Type> = {
    ftv = fun t ->
        match t with
        | TVar n -> Set.singleton n
        | TInt -> Set.empty
        | TBool -> Set.empty
        | TFun (t1, t2) -> Set.union (typeTypes.ftv t1) (typeTypes.ftv t2)

    apply = fun s t ->
        match t with
        | TVar n -> 
            match Map.tryFind n s with
            | None -> t
            | Some t' -> t'
        | TInt -> TInt
        | TBool -> TBool
        | TFun (t1, t2) -> TFun (typeTypes.apply s t1, typeTypes.apply s t2)
}

let rec substTypes : Types<Scheme> = {
    ftv = fun (Scheme (vars, t)) -> Set.difference (typeTypes.ftv t) (Set.ofList vars)

    apply = fun s (Scheme (vars, t)) -> Scheme (vars, typeTypes.apply (List.foldBack Map.remove vars s) t)
}

let rec listTypes (types : Types<'a>) : Types<List<'a>> = {
    ftv = fun l -> List.foldBack Set.union (List.map types.ftv l) Set.empty

    apply = fun s l -> List.map (types.apply s) l
}

