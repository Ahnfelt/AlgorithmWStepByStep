module AlgorithmW.Inference

type Lit =
    | LInt of int
    | LBool of bool

type Exp =
    | EVar of string
    | ELit of Lit
    | EApp of Exp * Exp
    | EAbs of string * Exp
    | ELet of string * Exp * Exp

type Type =
    | TVar of string
    | TInt
    | TBool
    | TFun of Type * Type

type Scheme = Scheme of List<string> * Type

type Subst = Map<string, Type>

exception TypeError of string * Type * Type

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

let schemeTypes : Types<Scheme> = {
    ftv = fun (Scheme (vars, t)) -> Set.difference (typeTypes.ftv t) (Set.ofList vars)

    apply = fun s (Scheme (vars, t)) -> Scheme (vars, typeTypes.apply (List.foldBack Map.remove vars s) t)
}

let listTypes (types : Types<'a>) : Types<List<'a>> = {
    ftv = fun l -> List.foldBack Set.union (List.map types.ftv l) Set.empty

    apply = fun s l -> List.map (types.apply s) l
}

let nullSubst : Subst = Map.empty
let composeSubst (s1 : Subst) (s2 : Subst) : Subst =
    let s2' : Subst = Map.map (fun _ t -> typeTypes.apply s1 t) s2 
    List.fold (fun s (k, v) -> Map.add k v s) s1 (Map.toList s2') 

type TypeEnv = Map<string, Scheme>

let typeEnvTypes : Types<TypeEnv> = {
    ftv = fun env -> (listTypes schemeTypes).ftv (List.map snd (Map.toList env))
    apply = fun s env -> Map.map (fun _ scheme -> schemeTypes.apply s scheme) env
}

let generalize env t =
    let vars = Set.toList (Set.difference (typeTypes.ftv t) (typeEnvTypes.ftv env)) 
    Scheme (vars, t)

// A minor difference here is that we use effects instead of monads to generate the fresh type variables
let instantiate newTyVar (Scheme (vars, t)) : Type =
    let nvars = List.map (fun _ -> newTyVar "a") vars
    let s = Map.ofList (List.zip vars nvars)
    typeTypes.apply s t

// A minor difference here is that mgu is pure and doesn't need a monad
let rec mgu t1 t2 =
    let varBind u t =
        if t = TVar u then nullSubst
        else if Set.contains u (typeTypes.ftv t) then raise (TypeError ("occurs check fails", TVar u, t))
        else Map.add u t Map.empty
    match (t1, t2) with
    | (TFun (l, r), TFun (l', r')) -> 
        let s1 = mgu l l'
        let s2 = mgu r r'
        composeSubst s1 s2
    | (TVar u, t) | (t, TVar u) -> varBind u t
    | (TInt, TInt) -> nullSubst
    | (TBool, TBool) -> nullSubst
    | _ -> raise (TypeError ("types do not unify", t1, t2))

// A minor difference her is that we use some local state instead of a state monad
let typeInference env e =
    let tiSupply = ref 0
    let newTyVar prefix =
        let i = !tiSupply
        tiSupply := i + 1
        TVar (prefix + i.ToString())
    let rec ti env e  =
        match e with
        | EVar n -> 
            match Map.tryFind n env with
            | None -> failwith ("unbound variable" + n.ToString())
            | Some sigma -> (nullSubst, instantiate newTyVar sigma)
        | ELit (LBool _) -> (nullSubst, TBool)
        | ELit (LInt _) -> (nullSubst, TInt)
        | EAbs (n, e) ->
            let tv = newTyVar "a"
            let env' = Map.remove n env
            let env'' = Map.add n (Scheme ([], tv)) env'
            let (s1, t1) = ti env'' e
            (s1, TFun (typeTypes.apply s1 tv, t1))
        | EApp (e1, e2) ->
            let tv = newTyVar "a"
            let (s1, t1) = ti env e1
            let (s2, t2) = ti (typeEnvTypes.apply s1 env) e2
            let s3 = mgu (typeTypes.apply s2 t1) (TFun (t2, tv))
            (composeSubst (composeSubst s3 s2) s1, typeTypes.apply s3 tv)
        | ELet (x, e1, e2) ->
            let env' = Map.remove x env
            let (s1, t1) = ti env e1
            let t' = generalize (typeEnvTypes.apply s1 env) t1
            let env'' = Map.add x t' env'
            let (s2, t2) = ti (typeEnvTypes.apply s1 env'') e2
            (composeSubst s1 s2, t2)
    let (s, t) = ti env e
    typeTypes.apply s t
