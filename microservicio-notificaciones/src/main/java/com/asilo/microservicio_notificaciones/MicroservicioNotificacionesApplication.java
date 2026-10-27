
package com.asilo.microservicio_notificaciones;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.mail.SimpleMailMessage;
import org.springframework.mail.javamail.JavaMailSender;
import org.springframework.web.bind.annotation.*;

import java.util.HashMap;
import java.util.Map;

@SpringBootApplication
@RestController
@RequestMapping("/api/v1")
public class MicroservicioNotificacionesApplication {

    private final JavaMailSender mailSender;

    @Value("${spring.mail.username}")
    private String correoRemitente;

    public MicroservicioNotificacionesApplication(
            JavaMailSender mailSender) {
        this.mailSender = mailSender;
    }

    public static void main(String[] args) {
        SpringApplication.run(
            MicroservicioNotificacionesApplication.class,
            args
        );
    }

    // Datos que recibimos desde el sistema
    public static class NotificacionDTO {
        public String correoFamiliar;
        public String nombreFamiliar;
        public String nombrePaciente;
        public String medicoReferido;
        public String estadoPaciente;
    }

    // Comprobar que el microservicio funciona
    @GetMapping("/status")
    public Map<String, String> status() {

        Map<String, String> respuesta = new HashMap<>();

        respuesta.put(
            "servicio",
            "Microservicio de Notificaciones - Asilo Cabeza de Algodón"
        );

        respuesta.put(
            "estado",
            "Activo y funcionando en Java"
        );

        return respuesta;
    }

    // Enviar un correo real al familiar
    @PostMapping("/notificar-familiar")
    public ResponseEntity<Map<String, Object>> enviarNotificacion(
            @RequestBody NotificacionDTO datos) {

        Map<String, Object> respuesta = new HashMap<>();

        if (datos == null ||
            datos.correoFamiliar == null ||
            datos.correoFamiliar.isBlank() ||
            datos.nombreFamiliar == null ||
            datos.nombrePaciente == null ||
            datos.medicoReferido == null ||
            datos.estadoPaciente == null) {

            respuesta.put("status", "ERROR");
            respuesta.put("codigo", 400);
            respuesta.put("mensaje", "Faltan datos para enviar la notificación.");

            return ResponseEntity
                .badRequest()
                .body(respuesta);
        }

        String asunto =
            "Notificación Médica - Paciente: " + datos.nombrePaciente;

        String cuerpo =
            "Estimado/a " + datos.nombreFamiliar + ",\n\n" +
            "Le informamos sobre el estado actual del paciente " +
            datos.nombrePaciente + ".\n\n" +
            "Estado: " + datos.estadoPaciente + "\n" +
            "Ha sido referido con el médico especialista: " +
            datos.medicoReferido + ".\n\n" +
            "Atentamente,\n" +
            "Administración del Asilo de Ancianos Cabeza de Algodón";

        try {

            SimpleMailMessage correo = new SimpleMailMessage();

            correo.setFrom(correoRemitente);
            correo.setTo(datos.correoFamiliar);
            correo.setSubject(asunto);
            correo.setText(cuerpo);

            // Aquí se envía realmente el correo mediante Gmail
            mailSender.send(correo);

            respuesta.put("status", "EXITO");
            respuesta.put("codigo", 200);
            respuesta.put(
                "mensaje",
                "Correo enviado a " + datos.correoFamiliar
            );

            Map<String, String> detalle = new HashMap<>();

            detalle.put("destinatario", datos.correoFamiliar);
            detalle.put("asunto", asunto);
            detalle.put("mensaje_enviado", cuerpo);

            respuesta.put("detalle", detalle);

            return ResponseEntity.ok(respuesta);

        } catch (Exception e) {

            System.err.println(
                "Error al enviar correo: " + e.getMessage()
            );

            respuesta.put("status", "ERROR");
            respuesta.put("codigo", 500);
            respuesta.put(
                "mensaje",
                "No se pudo enviar el correo. Revise la configuración de Gmail."
            );

            return ResponseEntity
                .status(HttpStatus.INTERNAL_SERVER_ERROR)
                .body(respuesta);
        }
    }
}
