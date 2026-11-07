namespace Tesela.Gameplay
{
    /// <summary>Pieza en la bandeja. Shape == null significa casilla vacía.</summary>
    public struct TrayPiece
    {
        public PieceShape Shape;
        public byte Glaze;
        public bool Golden;

        public bool IsEmpty => Shape == null;

        public TrayPiece(PieceShape shape, byte glaze, bool golden = false)
        {
            Shape = shape;
            Glaze = glaze;
            Golden = golden;
        }

        public static readonly TrayPiece None = default;
    }
}
