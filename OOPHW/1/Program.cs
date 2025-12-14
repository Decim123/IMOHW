class Car
{
    public string Brand { get; private set; }
    public string Model { get; private set; }
    public int Distance { get; private set; }

    public Car(string brand, string model, int distance)
    {
        Brand = brand;
        Model = model;
        Distance = distance;
    }

    public void Drive(int distance)
    {
        if (distance > 0)
        {
            Distance += distance;
        }
    }

    public override string ToString()
    {
        return $"Машина: {Brand} {Model}, пробег: {Distance} km";
    }
}

class Program
{
    static void Main()
    {
        Car car = new Car("Toyota", "sequoia", 0);

        Console.WriteLine(car);
        Console.WriteLine("вызов Drive(10):");

        car.Drive(10);

        Console.WriteLine(car);
    }
}
