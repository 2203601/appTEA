-- Datos fijos de los juegos. Los UUID son constantes para que la app los use directamente.
insert into public.games (id, name, description, category, max_levels, is_cooperative, is_active) values
    ('00000000-0000-0000-0000-000000000001', 'memoria',    'Encontrar los pares de cartas',        'cognitivo', 3, false, true),
    ('00000000-0000-0000-0000-000000000002', 'encajar',    'Encajar piezas en su silueta',          'cognitivo', 3, false, true),
    ('00000000-0000-0000-0000-000000000003', 'completar',  'Completar palabras o numeros',          'lenguaje',  3, false, true),
    ('00000000-0000-0000-0000-000000000004', 'linea',      'Escribir siguiendo la linea',           'motricidad', 3, false, true),
    ('00000000-0000-0000-0000-000000000005', 'emociones',  'Identificar emociones',                 'social',    3, false, true),
    ('00000000-0000-0000-0000-000000000006', 'objetos',    'Identificar objetos (dos jugadores)',   'social',    3, true,  false)
on conflict (id) do nothing;
