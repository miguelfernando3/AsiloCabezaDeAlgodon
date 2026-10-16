package com.asilo.microservicio_notificaciones;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.web.bind.annotation.*;

import java.util.HashMap;
import java.util.Map;

@SpringBootApplication
@RestController
@RequestMapping("/api/v1")
public class MicroservicioNotificacionesApplication {

    public static void main(String[] args) {
        SpringApplication.run(MicroservicioNotificacionesApplication.class, args);
    }

    // Estructura de datos que recibe la petición
    public static class NotificacionDTO {
        public String correoFamiliar;
        public String nombreFamiliar;
        public String nombrePaciente;
        public String medicoReferido;
        public String estadoPaciente;
    }

    // Endpoint de prueba de vida (Healthcheck)
    @GetMapping("/status")
    public Map<String, String> status() {
        Map<String, String> response = new HashMap<>();
        response.put("servicio", "Microservicio de Notificaciones - Asilo Cabeza de Algodón");
        response.put("estado", "Activo y funcionando en Java");
        return response;
    }

    // Endpoint principal del microservicio
    @PostMapping("/notificar-familiar")
    public Map<String, Object> enviarNotificacion(@RequestBody NotificacionDTO datos) {
        
        String asunto = "Notificación Médica - Paciente: " + datos.nombrePaciente;
        String cuerpo = "Estimado/a " + datos.nombreFamiliar + ",\n\n" +
                        "Le informamos sobre el estado actual del paciente " + datos.nombrePaciente + ".\n" +
                        "Estado: " + datos.estadoPaciente + "\n" +
                        "Ha sido referido con el médico especialista: " + datos.medicoReferido + ".\n\n" +
                        "Atentamente,\nAdministración del Asilo de Ancianos";

        Map<String, Object> respuesta = new HashMap<>();
        respuesta.put("status", "EXITO");
         respuesta.put("codigo", 200);
        respuesta.put("mensaje", "Notificación procesada y enviada a " + datos.correoFamiliar);

        Map<String, String> detalle = new HashMap<>();
        detalle.put("destinatario", datos.correoFamiliar);
        detalle.put("asunto", asunto);
        detalle.put("mensaje_enviado", cuerpo);

        respuesta.put("detalle", detalle);

        return respuesta;
    }
}