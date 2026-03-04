using solverTest.Models;

namespace solverTest.Scheduler
{
    public class RandomSearchScheduler
    {
        private readonly List<Agent> _agents;
        private readonly List<PalletRoute> _routes;
        private readonly Dictionary<int, List<int>> _assignment;

        private readonly Dictionary<string, double> _itemWeight;

        public RandomSearchScheduler(
            List<Agent> agents,
            List<PalletRoute> routes,
            Dictionary<int, List<int>> assignment,
            Dictionary<string, double> itemWeight)
        {
            _agents = agents;
            _routes = routes;
            _assignment = assignment;
            _itemWeight = itemWeight;
        }

        public SchedulerResult Optimize(int iterations)
        {
            List<ScheduleItem> best = null;
            double bestTime = double.MaxValue;

            for (int i = 0; i < iterations; i++)
            {
                var order = GenerateHybridOrder();
                var schedule = BuildSchedule(order);
                double makespan = schedule.Max(s => s.End);

                if (makespan < bestTime)
                {
                    bestTime = makespan;
                    best = schedule;
                }
            }
                       
            var humanEnergy = new Dictionary<int, double>();

            foreach (var agent in _agents.Where(a => a.Type == "human"))
            {
                double totalEnergy = 0;

                var tasks = best.Where(s => s.AgentId == agent.Id && s.ItemId != "RD");

                foreach (var task in tasks)
                {
                    totalEnergy += ComputeHumanEnergy(task, agent);
                }

                humanEnergy[agent.Id] = totalEnergy;
            }

            double averageEnergy = humanEnergy.Any()
                ? humanEnergy.Values.Average()
                : 0;

            return new SchedulerResult
            {
                BestSchedule = best,
                Makespan = bestTime,
                AverageHumanEnergy = averageEnergy,
                HumanEnergyPerAgent = humanEnergy
            };
        }


        private Dictionary<int, List<int>> GenerateHybridOrder()
        {
            var result = new Dictionary<int, List<int>>();

            // calculate global item frequency
            var globalItemFrequency = new Dictionary<string, int>();

            foreach (var route in _routes)
            {
                var distinctItems = route.Segments
                                         .Select(s => s.ItemId)
                                         .Distinct();

                foreach (var item in distinctItems)
                {
                    if (!globalItemFrequency.ContainsKey(item))
                        globalItemFrequency[item] = 0;

                    globalItemFrequency[item]++;
                }
            }

            // scored pallet list for each agent
            foreach (var agent in _agents)
            {
                if (!_assignment.ContainsKey(agent.Id))
                    continue;

                var scoredPallets = new List<(int palletIndex, double score)>();

                foreach (var palletIndex in _assignment[agent.Id])
                {
                    var route = _routes[palletIndex];

                    var distinctItems = route.Segments
                                             .Select(s => s.ItemId)
                                             .Distinct()
                                             .ToList();

                    // Conflict Score
                    double conflictScore = distinctItems
                        .Sum(item => globalItemFrequency[item]);

                    // Critical Score (high frequency itemler)
                    double criticalScore = distinctItems
                        .Sum(item => globalItemFrequency[item] >= 3
                                        ? globalItemFrequency[item]
                                        : 0);

                    // Pallet Duration
                    double palletDuration = route.Segments.Sum(s =>
                                                            s.Distance / agent.Speed +
                                                            s.Quantity * agent.PickTime
                                                        );


                    // Hybrid Score
                    double totalScore =
                        2.0 * criticalScore +
                        1.0 * conflictScore +
                        0.5 * palletDuration;

                    scoredPallets.Add((palletIndex, totalScore));
                }

                // before small skor
                result[agent.Id] = scoredPallets
                    .OrderBy(x => x.score)
                    .Select(x => x.palletIndex)
                    .ToList();
            }

            return result;
        }


        private List<ScheduleItem> BuildSchedule(
    Dictionary<int, List<int>> agentPalletOrder)
        {
            var schedule = new List<ScheduleItem>();

            var agentAvailable = _agents.ToDictionary(a => a.Id, a => 0.0);
            var resourceAvailable = new Dictionary<string, double>();

            var agentPointers = new Dictionary<int, (int palletPtr, int segmentPtr)>();

            foreach (var agent in _agents)
                agentPointers[agent.Id] = (0, 0);

            while (true)
            {
                var eventList = new List<(int AgentId,
                                          int PalletIdx,
                                          int SegmentIdx,
                                          string ItemId,
                                          double ArrivalTime,
                                          double ProcessDuration,
                                          int Quantity)>();

                
                foreach (var agent in _agents)
                {
                    if (!agentPalletOrder.ContainsKey(agent.Id))
                        continue;

                    var palletList = agentPalletOrder[agent.Id];
                    var (pPtr, sPtr) = agentPointers[agent.Id];

                    if (pPtr >= palletList.Count)
                        continue;

                    var route = _routes[palletList[pPtr]];

                    // Depot dönüş ayrı ele alınacak
                    if (sPtr >= route.Segments.Count)
                    {
                        double returnTime = palletList[pPtr] <= 25 ? 20.0 : 40.0;

                        eventList.Add((
                            agent.Id,
                            palletList[pPtr],
                            -1,
                            "RD",
                            agentAvailable[agent.Id],
                            returnTime,
                            0
                        ));

                        continue;
                    }

                    var seg = route.Segments[sPtr];

                    double arrival = agentAvailable[agent.Id];
                    double duration =
                        seg.Distance / agent.Speed +
                        seg.Quantity * agent.PickTime;

                    eventList.Add((
                        agent.Id,
                        palletList[pPtr],
                        sPtr,
                        seg.ItemId,
                        arrival,
                        duration,
                        seg.Quantity
                    ));
                }

                if (eventList.Count == 0)
                    break;

                // PRIORITY LOGIC
                var nextEvent = eventList
                    .OrderBy(e => e.ArrivalTime)
                    .ThenBy(e => e.ProcessDuration)
                    .First();

                double start = nextEvent.ArrivalTime;

                if (nextEvent.ItemId != "RD")
                {
                    if (!resourceAvailable.ContainsKey(nextEvent.ItemId))
                        resourceAvailable[nextEvent.ItemId] = 0;

                    start = Math.Max(start, resourceAvailable[nextEvent.ItemId]);
                }

                double waiting = start - nextEvent.ArrivalTime;
                double end = start + nextEvent.ProcessDuration;

                schedule.Add(new ScheduleItem
                {
                    AgentId = nextEvent.AgentId,
                    AgentType = _agents.First(a => a.Id == nextEvent.AgentId).Type,
                    PalletIndex = nextEvent.PalletIdx,
                    ItemId = nextEvent.ItemId,
                    Start = start,
                    End = end,
                    WaitingTime = waiting,
                    Quantity = nextEvent.Quantity
                });

                agentAvailable[nextEvent.AgentId] = end;

                if (nextEvent.ItemId != "RD")
                    resourceAvailable[nextEvent.ItemId] = end;

                // Pointer ilerlet
                var pointer = agentPointers[nextEvent.AgentId];
                int palletPtr = pointer.palletPtr;
                int segmentPtr = pointer.segmentPtr;


                if (nextEvent.ItemId == "RD")
                    agentPointers[nextEvent.AgentId] = (palletPtr + 1, 0);
                else
                    agentPointers[nextEvent.AgentId] = (palletPtr, segmentPtr + 1);

            }

            return schedule;
        }



        private const double BW = 75.0; 

        private double ComputeHumanEnergy(ScheduleItem s, Agent agent)
        {
            double duration = s.End - s.Start - s.WaitingTime;

            if (duration <= 0)
                return 0;

            double speed = agent.Speed;

            // item weight 
            double weight = 1.0; // default

            if (_itemWeight != null && _itemWeight.ContainsKey(s.ItemId))
                weight = _itemWeight[s.ItemId];

            double totalLoad = weight * s.Quantity;

            // Travel Energy 
            double E_travel =
                0.51 +
                (0.02584 * BW * speed * speed) *
                (duration / 60.0);

            // Pick Energy 
            double E_pick =
                0.424177 +
                0.0232911 * totalLoad * (duration / 60.0);

            return E_travel + E_pick;
        }




    }


}
