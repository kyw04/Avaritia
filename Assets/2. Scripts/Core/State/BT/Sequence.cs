namespace BT
{
    public class Sequence : CompositeNode
    {
        public Sequence(params Node[] children) : base(children) { }

        public override NodeStatus Evaluate()
        {
            foreach (var child in children)
            {
                var status = child.Evaluate();
                if (status != NodeStatus.Success)
                    return status;
            }
            return NodeStatus.Success;
        }
    }
}
