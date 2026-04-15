```mermaid
gantt
    title GANTT – App TEA | Proyecto UCC | Mar → Nov 2026
    dateFormat  YYYY-MM-DD
    axisFormat  %d/%m

    section FASE COMÚN
    Selección de temas y formación del equipo :2026-03-19, 2026-03-25
    Entrevista con Fonoaudióloga              :milestone, 2026-03-26, 0d
    Entrevista con Psicóloga                  :milestone, 2026-03-30, 0d
    Reunión con Débora                        :milestone, 2026-04-03, 0d
    Armado Anteproyecto (AP)                  :2026-04-06, 2026-04-15
    Entrega Anteproyecto                      :milestone, 2026-04-16, 0d

    section FASE VARIABLE
    Investigación TEA – bibliografía         :2026-04-17, 2026-04-30
    Análisis de requerimientos               :2026-04-17, 2026-04-30
    Reunión docente Débora Theaux            :milestone, 2026-04-20, 0d
    Criterios de accesibilidad y estímulos   :2026-05-01, 2026-05-07
    Diseño arquitectura microservicios       :2026-05-01, 2026-05-14
    Diagramas de arquitectura (C4)           :2026-05-08, 2026-05-14
    Definición de tecnología / stack         :2026-05-08, 2026-05-14
    Formación en tecnologías elegidas        :2026-05-15, 2026-05-25
    Repositorio / CI-CD pipeline             :2026-05-26, 2026-05-28
    Entornos: dev / staging / prod           :2026-05-26, 2026-05-28
    Diseño UX – wireframes                   :2026-05-26, 2026-06-08
    Mocks / prototipos interactivos          :2026-05-29, 2026-06-11
    Revisión mocks con docente               :milestone, 2026-06-02, 0d
    Módulo: Autenticación y perfiles         :2026-06-02, 2026-06-22
    Módulo: Desarrollo de juegos educativos  :2026-06-02, 2026-07-06
    Módulo: Adaptación / configuración       :2026-06-23, 2026-07-06
    MESA DE EXÁMENES – pausa                 :crit, 2026-07-07, 2026-07-20
    Módulo: Registro de progreso             :2026-07-07, 2026-07-20
    Panel de seguimiento (padres/prof.)      :2026-07-21, 2026-08-03
    Check 1 de avances – 4/8                 :milestone, 2026-08-04, 0d

    section TESTING
    Plan de testing y casos de prueba        :2026-07-21, 2026-07-27
    Testing unitario e integración           :2026-08-04, 2026-08-17
    Testing accesibilidad / TEA              :2026-08-18, 2026-08-31
    Testing end-to-end y regresión           :2026-09-01, 2026-09-07
    Bug fixing sprint                        :2026-09-08, 2026-09-14

    section CIERRE
    80% avance – demo interna                :2026-09-15, 2026-09-28
    Informe: Diagnóstico / Marco Teórico     :2026-09-29, 2026-10-19
    Ajustes finales y pulido UX/UI           :2026-10-20, 2026-11-02
    Check 2 de avances – 3/11                :milestone, 2026-11-03, 0d
    Entrega final / Presentación             :milestone, 2026-11-03, 0d
