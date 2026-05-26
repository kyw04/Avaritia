public class ComboBuffer
{
    private bool hasBufferedInput = false;
    private float bufferTimer = 0f;
    private float bufferWindow = 0.3f;

    public void RegisterInput()
    {
        hasBufferedInput = true;
        bufferTimer = bufferWindow;
    }

    public bool ConsumeInput()
    {
        if (hasBufferedInput)
        {
            hasBufferedInput = false;
            return true;
        }
        return false;
    }

    public void Tick(float delta)
    {
        if (bufferTimer > 0f)
        {
            bufferTimer -= delta;
            if (bufferTimer <= 0f)
                hasBufferedInput = false;
        }
    }
}