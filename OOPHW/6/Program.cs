using System;

enum CreditBand
{
    A,
    B,
    C,
    D,
    GovGuaranteed
}

sealed class LoanContext
{
    public bool HasCollateral { get; }
    public bool IsFirstTime { get; }
    public bool HasPromo { get; }

    public LoanContext(bool hasCollateral, bool isFirstTime, bool hasPromo)
    {
        HasCollateral = hasCollateral;
        IsFirstTime = isFirstTime;
        HasPromo = hasPromo;
    }
}

static class RateRules
{
    public const int CollateralDelta = -3;
    public const int FirstTimeDelta = 2;
    public const int PromoDelta = -2;

    public static int ApplyModifiers(int baseRate, LoanContext ctx)
    {
        int rate = baseRate;

        if (ctx.HasCollateral)
            rate += CollateralDelta;

        if (ctx.IsFirstTime)
            rate += FirstTimeDelta;

        if (ctx.HasPromo)
            rate += PromoDelta;

        if (rate < 0)
            rate = 0;

        return rate;
    }
}

static class InterestCalculatorEnum
{
    public static int GetRate(CreditBand band, LoanContext ctx)
    {
        if (band == CreditBand.GovGuaranteed)
            return 3;

        int baseRate = band switch
        {
            CreditBand.A => 8,
            CreditBand.B => 12,
            CreditBand.C => 18,
            CreditBand.D => 25,
            _ => throw new ArgumentOutOfRangeException(nameof(band))
        };

        return RateRules.ApplyModifiers(baseRate, ctx);
    }
}

abstract class BorrowerBand
{
    private readonly int baseRate;

    public int BaseRate => baseRate;

    protected BorrowerBand(int baseRate)
    {
        if (baseRate < 0)
            throw new ArgumentOutOfRangeException(nameof(baseRate));

        this.baseRate = baseRate;
    }

    public virtual int GetRate(LoanContext ctx)
    {
        return BaseRate;
    }
}

sealed class BandA : BorrowerBand
{
    public BandA() : base(8) { }

    public override int GetRate(LoanContext ctx)
    {
        return RateRules.ApplyModifiers(BaseRate, ctx);
    }
}

sealed class BandB : BorrowerBand
{
    public BandB() : base(12) { }

    public override int GetRate(LoanContext ctx)
    {
        return RateRules.ApplyModifiers(BaseRate, ctx);
    }
}

sealed class BandC : BorrowerBand
{
    public BandC() : base(18) { }

    public override int GetRate(LoanContext ctx)
    {
        return RateRules.ApplyModifiers(BaseRate, ctx);
    }
}

sealed class BandD : BorrowerBand
{
    public BandD() : base(25) { }

    public override int GetRate(LoanContext ctx)
    {
        return RateRules.ApplyModifiers(BaseRate, ctx);
    }
}

sealed class GovGuaranteed : BorrowerBand
{
    public GovGuaranteed() : base(3) { }
}

static class InterestCalculatorOop
{
    public static int GetRate(BorrowerBand band, LoanContext ctx)
    {
        if (band == null)
            throw new ArgumentNullException(nameof(band));

        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        return band.GetRate(ctx);
    }
}

static class Program
{
    static void Main()
    {
        var ctxNone = new LoanContext(false, false, false);
        var ctxColl = new LoanContext(true, false, false);
        var ctxFirst = new LoanContext(false, true, false);
        var ctxPromoColl = new LoanContext(true, false, true);
        var ctxAny = new LoanContext(true, true, true);

        TestEnum("A без модификаторов", CreditBand.A, ctxNone, 8);
        TestEnum("B с обеспечением", CreditBand.B, ctxColl, 9);
        TestEnum("C первый раз", CreditBand.C, ctxFirst, 20);
        TestEnum("D промо+обеспечение", CreditBand.D, ctxPromoColl, 20);
        TestEnum("GovGuaranteed любой", CreditBand.GovGuaranteed, ctxAny, 3);

        Console.WriteLine();

        TestOop("A без модификаторов", new BandA(), ctxNone, 8);
        TestOop("B с обеспечением", new BandB(), ctxColl, 9);
        TestOop("C первый раз", new BandC(), ctxFirst, 20);
        TestOop("D промо+обеспечение", new BandD(), ctxPromoColl, 20);
        TestOop("GovGuaranteed любой", new GovGuaranteed(), ctxAny, 3);
    }

    static void TestEnum(
        string name,
        CreditBand band,
        LoanContext ctx,
        int expected)
    {
        int got = InterestCalculatorEnum.GetRate(band, ctx);
        Print(name, got, expected);
    }

    static void TestOop(
        string name,
        BorrowerBand band,
        LoanContext ctx,
        int expected)
    {
        int got = InterestCalculatorOop.GetRate(band, ctx);
        Print(name, got, expected);
    }

    static void Print(string name, int got, int expected)
    {
        string ok = got == expected ? "OK" : "FAIL";
        Console.WriteLine($"{name}: ставка {got}% (ожид. {expected}%) -> {ok}");
    }
}
