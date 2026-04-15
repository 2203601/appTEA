```mermaid
gantt
    title GANTT – App TEA | Proyecto UCC
    dateFormat  YYYY-MM-DD
    axisFormat  %d/%m

    section FASE INICIAL
    Selección de tema y equipo      :a1, 2026-03-19, 6d
    Entrevistas profesionales       :a2, after a1, 8d
    Armado anteproyecto             :a3, after a2, 10d
    Entrega anteproyecto            :milestone, after a3, 0d

    section ANÁLISIS E INVESTIGACIÓN
    Investigación TEA               :b1, after a3, 10d
    Análisis de requerimientos      :b2, after a3, 10d
    Criterios accesibilidad         :b3, after b1, 7d

    section ARQUITECTURA Y TECNOLOGÍA
    Diseño arquitectura             :c1, after b2, 10d
    Diagramas (C4)                  :c2, after c1, 7d
    Definición de tecnologías       :c3, after c1, 7d
    Formación tecnologías           :c4, after c3, 10d

    section PREPARACIÓN TÉCNICA
    Repo + branching + CI/CD        :d1, after c3, 3d
    Entornos (dev/staging/prod)     :d2, after d1, 3d

    section DISEÑO UX/UI
    Wireframes                      :e1, after c3, 10d
    Mocks interactivos              :e2, after e1, 10d
    Validación con docente          :milestone, after e2, 0d

    section DESARROLLO (SPRINTS)
    Autenticación y perfiles        :f1, after c4, 14d
    Juegos educativos               :f2, after c4, 21d
    Configuración adaptativa        :f3, after f1, 10d

    section PAUSA ACADÉMICA
    Mesa de exámenes                :crit, after f2, 14d

    section DESARROLLO AVANZADO
    Registro de progreso            :g1, after f3, 10d
    Panel padres/docentes           :g2, after g1, 10d
    Checkpoint avance               :milestone, after g2, 0d

    section TESTING
    Plan de testing                 :h1, after g2, 7d
    Testing integración             :h2, after h1, 10d
    Testing accesibilidad TEA       :h3, after h2, 10d
    Testing end-to-end              :h4, after h3, 7d
    Bug fixing                     :h5, after h4, 7d

    section CIERRE
    Demo interna (80%)              :i1, after h5, 10d
    Documentación final             :i2, after i1, 15d
    Ajustes finales UX/UI           :i3, after i2, 10d
    Entrega final                   :milestone, after i3, 0d
