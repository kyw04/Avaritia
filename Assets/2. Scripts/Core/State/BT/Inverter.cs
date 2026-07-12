namespace BT
{
    public class Inverter : Node
    {
        private readonly Node child;

        public Inverter(Node child)
        {
            this.child = child;
        }

        public override NodeStatus Evaluate()
        {
            return child.Evaluate() switch
            {
                NodeStatus.Success => NodeStatus.Failure,
                NodeStatus.Failure => NodeStatus.Success,
                _ => NodeStatus.Running
            };
        }
    }
}
