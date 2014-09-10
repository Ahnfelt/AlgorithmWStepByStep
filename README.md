AlgorithmWStepByStep
====================

Type inference for ML-like languages. A port to F# of "Algorithm W Step by Step" by Martin Grabmüller. If there are bugs, they're probably mine.

I've kept the variable names from the article, to make it easier to follow this code while reading the article.

The only major difference is that the F# port uses local state for the supply of fresh variables rather than a monad.

Please see http://www.grabmueller.de/martin/www/pub/AlgorithmW.pdf for documentation.


The problem in a nutshell
-------------------------

- What is the type of the empty list `[]`?
  - It's a list with **some type of elements**.

- What is the type of the function that appends two lists?
  - It takes a list with **some type of elements** and 
  - another list with **that same type of elements**, and 
  - returns a list with **that same type of elements**.
 
- What is the type of the identity function `id x = x`?
  - It takes **any type of value** and 
  - returns **that same type of value**.

The solution
------------

- To represent **"some type"**, we use *type variables*.
- To represent **"that same type"**, we use *equality constraints* between types.
- To represent **"any type"**, we use *type schemes*.


The code
--------

The code contains an abstract syntax tree of a language with `lambda`-functions (`EAbs`/`EApp`), polymorphic `let`-bindings (`ELet`) and some literals for primitive types (`ELit`).

The types are also represented as an abstract syntax tree, with type variables (`TVar`) and types for functions (`TFun`) and primitive types (`TInt`/`TBool`), and type variables.

Since some functions can work with many different types (eg. they are *polymorphic*), we represent the types that can vary like that with type variables that are quantified with `forall`. This is often called a type scheme (`Scheme`). In standard Hindley-Milner, type schemes can only occur in the type environment (`TypeEnv`), which binds variables to type schemes. 

When solving constraints, the solution is a *substitution* (`Subst`) that binds type variables to types. For things that contain types, like type environments, type schemes and types themselves, we need to be able to `apply` this subsitution, replacing the bound type variables with the types they are bound to. For the next step, we also need to be able to find the free type variables (`ftv`). These operations are represented by the record called `Types`, which has instances for each of the type containers.

Whenever we let-bind a variable, we `generalize` the type to introduce the *forall* for type variables that are not constrained, and thus can be quantified in a type scheme. Since we solve constraints as soon as we generate them, the only type variables that may be constrained in the future are those in the type environment. These won't be qantified with a *forall*.

When we mention a variable, we immediately `instantiate` the type scheme it's bound to. This is simply done by replacing all the quantified variables with fresh type variables (`newTyVar`).

To solve an equality constraint `t1 = t2`, we must find the most general unifier (`mgu`), which is a substitution that when applied to `t1` and `t2` makes the two types *syntactically* equal. When this is not possible, eg. `TInt = TBool`, we raise a `TypeError`.

The type inference function recursively visits an expression, generating equality constraints that are immediately solved with `mgu`. The unification results in a substitution that is then returned, and finally applied to the resulting type. In order to generate fresh type variables (`newTyVar`), we simply increment a local mutable variable (`tiSupply`).
