namespace BT
{
    public class BehaviorTree : IAIController
    {
        private readonly Node root;
        public Blackboard Board { get; }
        public NodeStatus LastStatus { get; private set; }

        public BehaviorTree(Node root, Blackboard board)
        {
            this.root = root;
            Board = board;
        }

        public void Init()
        {
            StateManager.Instance.Register(this);
        }

        public void Execute()
        {
            LastStatus = root.Evaluate();
        }

        public void FixedExecute() { }
    }
}
