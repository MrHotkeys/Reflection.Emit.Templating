## Extension Methods
`EmitCode(this ILGenerator il, TypeBuilder typeBuilder, Func<EmitTemplateSurrogate, Delegate> callback)`

Emits instance method or constructor IL from a template delegate.

`EmitCodeStatic(this ILGenerator il, TypeBuilder typeBuilder, Func<EmitTemplateSurrogate, Delegate> callback)`

Emits static method IL from a template delegate.

## Template Surrogate
Extension methods take a `Func<EmitTemplateSurrogate, Delegate> callback` which creates the template delegate using a surrogate object for instance members (think `this`):

```
constructorIL.EmitCode(typeBuilder, surrogate => (int x, int y, int z) =>
{
    surrogate.Set<int>(x_LocalBuilder, x);
    surrogate.Set<int>(y_FieldBuilder, x);
    surrogate.Set<int>(z_PropertyBuilder, x);

    var sum = surrogate.Get<int>(x_LocalBuilder) + 
              surrogate.Get<int>(y_FieldBuilder) +
              surrogate.Get<int>(z_PropertyBuilder);

    surrogate.Call(print_MethodBuilder, sum);    
});
```
Overloads of `Get()` and `Set()` exist for `LocalVariableInfo`, `FieldInfo`, and `PropertyInfo`.

Overloads for `Call()` exist for up to six parameters, with and without a return type. The method to call can be given as a `MethodInfo`, `Action`, `Func`, or `Delegate`.

`Ref()` is also defined for `LocalVariableInfo` and `FieldInfo` to make ByRef values for use with `Call()` for methods that take `ref` or `out` parameters.

`Call()` can also be used to call static methods, either on the type being constructed or defined elsewhere.

When building for a method with _n_ parameters, the template method can define 0..._n_ parameters (but no more, and the types must match the method definition).

## Anonymous Method Captures
Captures are supported for simple and complex values:
```
var owls = "The owls are not what they seem";
var fireWalk = new StringBuilder()
    .AppendLine("Fire")
    .AppendLine("Walk")
    .Append("With Me");
methodIL.EmitCode(typeBuilder, surrogate => () => 
{
    this.Logger.LogWarning(owls);
    this.Logger.LogCritical(fireWalk.ToString());    
});
```
With support for capturing mutable structs:
```
struct Gum
{
    public bool YouLike;
    public bool GoingToComeBackInStyle;
    
    public override string ToString() =>
        $"{YouLike && GoingToComeBackInStyle}";
}

...

var gum = new Gum { YouLike = true, GoingToComeBackInStyle = false };
methodIL.EmitCode(typeBuilder, surrogate => () => 
{
    Console.WriteLine(gum);
    gum.GoingToComeBackInStyle = true;
    Console.WriteLine(gum);
});
```