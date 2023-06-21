using System;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Rolling.Utilities;

namespace Rolling.Tests;

public class MaybeAssertions<T> : MaybeAssertions<T, MaybeAssertions<T>>
{
    public MaybeAssertions(Maybe<T> value) : base(value)
    {
    }
}

public class MaybeAssertions<TSubject, TAssertion> : ObjectAssertions<Maybe<TSubject>, TAssertion>
    where TAssertion : ObjectAssertions<Maybe<TSubject>, TAssertion>
{
    public MaybeAssertions(Maybe<TSubject> value) : base(value)
    {
    }

    public AndConstraint<MaybeAssertions<TSubject, TAssertion>> Be(TSubject expected,
        string because = "",
        params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!Subject.IsNone)
            .WithDefaultIdentifier(Identifier)
            .FailWith("Expected {context} to be set {0}{reason}, but is None", expected);

        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Object.Equals(Subject.OrDefault(), expected))
            .WithDefaultIdentifier(Identifier)
            .FailWith("Expected {context} to be {0}{reason}, but found {1}.", expected, Subject.OrDefault());

        return new AndConstraint<MaybeAssertions<TSubject, TAssertion>>(this);
    }

    public AndConstraint<MaybeAssertions<TSubject, TAssertion>> BeNone(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(Subject.IsNone)
            .WithDefaultIdentifier(Identifier)
            .FailWith("Expected {context} to be None{reason}, but is {0}", Subject.OrDefault());

        return new AndConstraint<MaybeAssertions<TSubject, TAssertion>>(this);
    }

    public AndWhichConstraint<MaybeAssertions<TSubject, TAssertion>, TSubject> BeSet(string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!Subject.IsNone)
            .WithDefaultIdentifier(Identifier)
            .FailWith("Expected {context} to be set{reason}, but is None");

        return new AndWhichConstraint<MaybeAssertions<TSubject, TAssertion>, TSubject>(this, Subject.OrDefault());
    }
}