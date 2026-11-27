using System;

// Identificadores fijos de cada juego: tienen que coincidir con supabase/seed.sql
public static class IdsDeJuegos
{
    public static readonly Guid Memoria = new Guid("00000000-0000-0000-0000-000000000001");
    public static readonly Guid Encajar = new Guid("00000000-0000-0000-0000-000000000002");
    public static readonly Guid Completar = new Guid("00000000-0000-0000-0000-000000000003");
    public static readonly Guid Linea = new Guid("00000000-0000-0000-0000-000000000004");
    public static readonly Guid Emociones = new Guid("00000000-0000-0000-0000-000000000005");
    public static readonly Guid Objetos = new Guid("00000000-0000-0000-0000-000000000006");
}
