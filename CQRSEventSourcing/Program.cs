using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CQRSEventSourcing
{
    public class Person
    {
        private int age;
        EventBroker broker;
        public Person(EventBroker broker)
        {
            this.broker = broker;
            broker.Commands += BrokerOnCommands;
            broker.Queries += BrokerOnQueries;
        }

        private void BrokerOnQueries(object sender, Query query)
        {
            var ac = query as AgeQuery;
            if(ac != null && ac.Target == this)
            {
                ac.Result = age;
            }
        }

        private void BrokerOnCommands(object sender, Command command)
        {
            var cac = command as ChangeAgeCommand;
            if(cac != null && cac.Target == this)
            {
                if(cac.Register) broker.AllEvents.Add(new AgeChangedEvent(this, age, cac.Age));
                age = cac.Age;
            }
        }
        public bool CanVote => age >= 18;
    }
    public class EventBroker
    {
        // 1. All events that happened
        public IList<Event> AllEvents = new List<Event>();
        // 2. Commands
        public event EventHandler<Command> Commands;
        // 3. Query
        public event EventHandler<Query> Queries;

        public void Command(Command command)
        {
            Commands?.Invoke(this, command);
        }
        public T Query<T>(Query query)
        {
            Queries?.Invoke(this, query);
            return (T)query.Result;
        }
        public void UndoLast()
        {
            var e = AllEvents.LastOrDefault();
            var ac = e as AgeChangedEvent;
            if(ac != null)
            {
                Command(new ChangeAgeCommand(ac.Target, ac.OldValue) { Register = false});
                AllEvents.Remove(e);
            }
        }
    }

    public class Query
    {
        public object Result;
    }
    class AgeQuery : Query
    {
        public Person Target;
    }
    public class Command : EventArgs
    {
        public bool Register = true;
    }
    class ChangeAgeCommand : Command
    {
        public Person Target;
        public int Age;

        public ChangeAgeCommand(Person target, int age)
        {
            Target = target;
            Age = age;
        }
    }

    public class Event
    {

    }
    class AgeChangedEvent : Event
    {
        public Person Target;
        public int OldValue, NewValue;
        public AgeChangedEvent(Person target, int oldValue, int newValue)
        {
            Target = target;
            OldValue = oldValue;
            NewValue = newValue;
        }
        public override string ToString()
        {
            return $"Age changed from {OldValue} to {NewValue}";
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            var eventBroker = new EventBroker();
            var person = new Person(eventBroker);
            eventBroker.Command(new ChangeAgeCommand(person, 22));
            foreach(var e in eventBroker.AllEvents)
            {
                Console.WriteLine(e);
            }
            int age;
            age = eventBroker.Query<int>(new AgeQuery { Target = person });
            Console.WriteLine(age);
            eventBroker.UndoLast();
            foreach (var e in eventBroker.AllEvents)
            {
                Console.WriteLine(e);
            }
            age = eventBroker.Query<int>(new AgeQuery { Target = person });
            Console.WriteLine(age);
            Console.ReadKey();
        }
    }
}
