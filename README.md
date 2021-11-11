# Schedule

C# task scheduling for humans. Execute actions periodically using a friendly syntax. It is a port of the Java lib [schedule](https://github.com/SirLefti/schedule).

* chained syntax made for humans
* built-in scheduler
* no external dependencies

***

## Usage

```C#
using Scheduling;

public class Example {

    public static void Main(string[] args) {
        Action action = () => Console.WriteLine("Hello World!");

        Schedule.Every(10).Seconds().Run(action);
		Schedule.Every().Hour().At(":30").Run(action);
		Schedule.Every().Monday().At("00:30").Run(action);

		Schedule.Once().Monday().At("08:00").Run(action);
    }
}
```