namespace Tesela.Gameplay
{
    /// <summary>Estado especial de una celda ocupada (Restauración y eventos).</summary>
    public enum CellModifier : byte
    {
        None = 0,
        /// <summary>Baldosa agrietada: necesita dos limpiezas (vida en Board.Hp).</summary>
        Cracked = 1,
        /// <summary>Pieza bajo barro: se libera al limpiar su línea.</summary>
        Mud = 2,
        /// <summary>Azulejo dorado (evento): da puntos extra al limpiarse.</summary>
        Gold = 3,
        /// <summary>Bomba (evento): cuenta atrás en Board.Hp; si llega a 0 termina la partida.</summary>
        Bomb = 4
    }

    public struct ClearedCell
    {
        public int X;
        public int Y;
        public byte Glaze;
        public CellModifier Modifier;
        /// <summary>true si la celda sigue en el tablero (agrietada que solo perdió una vida).</summary>
        public bool Survived;
    }
}
