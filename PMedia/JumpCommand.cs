namespace PMedia;

public class JumpCommand
{
    public Direction direction;
    public int jump;

    public enum Direction
    {
        Backward = 0,
        Forward = 1
    }

    public JumpCommand(Direction direction, int jump)
    {
        this.direction = direction;
        this.jump = jump;
    }
}
