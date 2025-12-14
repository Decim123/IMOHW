abstract class Shape
{
    public abstract double Area();
}

class Square : Shape
{
    public double SideLength { get; }

    public Square(double sideLength)
    {
        SideLength = sideLength;
    }

    public override double Area()
    {
        return SideLength * SideLength;
    }
}

class Triangle : Shape
{
    public double Base { get; }
    public double Height { get; }

    public Triangle(double @base, double height)
    {
        Base = @base;
        Height = height;
    }

    public override double Area()
    {
        return Base * Height / 2;
    }
}

class Program
{
    static void Main()
    {
        Shape s1 = new Square(5);
        Shape s2 = new Triangle(6, 4);

        Console.WriteLine($"Площадь квадрата: {s1.Area()}");
        Console.WriteLine($"Площадь треугольника: {s2.Area()}");
    }
}
