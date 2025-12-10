using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using PumpRoomAutomationBackend.Models.Enums;

#nullable disable

namespace PumpRoomAutomationBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 创建 PostgreSQL 枚举类型
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    CREATE TYPE usergroup AS ENUM ('ROOT', 'ADMIN', 'OPERATOR', 'OBSERVER');
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;
            ");
            
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    CREATE TYPE userlevel AS ENUM ('LEVEL_1', 'LEVEL_2', 'LEVEL_3', 'LEVEL_4', 'LEVEL_5');
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;
            ");
            
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    CREATE TYPE userstatus AS ENUM ('ACTIVE', 'INACTIVE', 'PENDING', 'SUSPENDED');
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;
            ");
            
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    CREATE TYPE alarmseverity AS ENUM ('low', 'medium', 'high', 'critical');
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;
            ");
            
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    CREATE TYPE alarmstatus AS ENUM ('active', 'acknowledged', 'resolved', 'cleared');
                EXCEPTION
                    WHEN duplicate_object THEN null;
                END $$;
            ");

            migrationBuilder.CreateTable(
                name: "login_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    login_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "telemetry_minute",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    upstream_level = table.Column<double>(type: "double precision", nullable: true),
                    downstream_level = table.Column<double>(type: "double precision", nullable: true),
                    instantaneous_flow = table.Column<double>(type: "double precision", nullable: true),
                    flow_velocity = table.Column<double>(type: "double precision", nullable: true),
                    water_temperature = table.Column<double>(type: "double precision", nullable: true),
                    net_weight = table.Column<double>(type: "double precision", nullable: true),
                    speed = table.Column<double>(type: "double precision", nullable: true),
                    electric_current = table.Column<double>(type: "double precision", nullable: true),
                    winding_temperature = table.Column<double>(type: "double precision", nullable: true),
                    cabinet_outer_temperature = table.Column<double>(type: "double precision", nullable: true),
                    cabinet_inner_temperature = table.Column<double>(type: "double precision", nullable: true),
                    cabinet_outer_humidity = table.Column<double>(type: "double precision", nullable: true),
                    ts_minute = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry_minute", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    username = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    hashed_password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_group = table.Column<UserGroup>(type: "usergroup", nullable: false),
                    user_level = table.Column<UserLevel>(type: "userlevel", nullable: false),
                    operation_timeout = table.Column<int>(type: "integer", nullable: false),
                    operation_permissions = table.Column<string>(type: "text", nullable: true),
                    audit_permissions = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<UserStatus>(type: "userstatus", nullable: false),
                    full_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_admin = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "operational_parameters",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    set_velocity_high_limit = table.Column<double>(type: "double precision", nullable: true),
                    set_velocity_low_limit = table.Column<double>(type: "double precision", nullable: true),
                    set_m_velocity = table.Column<double>(type: "double precision", nullable: true),
                    set_velocity_alm = table.Column<double>(type: "double precision", nullable: true),
                    set_liquid_level_diff = table.Column<double>(type: "double precision", nullable: true),
                    set_p = table.Column<double>(type: "double precision", nullable: true),
                    set_i = table.Column<double>(type: "double precision", nullable: true),
                    set_d = table.Column<double>(type: "double precision", nullable: true),
                    motor_coli_heat_temp = table.Column<double>(type: "double precision", nullable: true),
                    motor_coli_stop_temp = table.Column<double>(type: "double precision", nullable: true),
                    motor_coli_alm_temp = table.Column<double>(type: "double precision", nullable: true),
                    motor_coli_cool_start_temp = table.Column<double>(type: "double precision", nullable: true),
                    motor_coli_cool_stop_temp = table.Column<double>(type: "double precision", nullable: true),
                    heating_is_running = table.Column<bool>(type: "boolean", nullable: false),
                    pump_run_time = table.Column<int>(type: "integer", nullable: true),
                    pump_stop_time = table.Column<int>(type: "integer", nullable: true),
                    alm_level_diff = table.Column<double>(type: "double precision", nullable: true),
                    alm_level_doppler_high = table.Column<double>(type: "double precision", nullable: true),
                    alm_flow_low = table.Column<double>(type: "double precision", nullable: true),
                    temp_max = table.Column<double>(type: "double precision", nullable: true),
                    temp_min = table.Column<double>(type: "double precision", nullable: true),
                    humidity_max = table.Column<int>(type: "integer", nullable: true),
                    humidity_min = table.Column<int>(type: "integer", nullable: true),
                    vibration_threshold = table.Column<double>(type: "double precision", nullable: true),
                    noise_threshold = table.Column<int>(type: "integer", nullable: true),
                    pressure = table.Column<double>(type: "double precision", nullable: true),
                    air_quality_threshold = table.Column<int>(type: "integer", nullable: true),
                    set_max_tare_weight = table.Column<double>(type: "double precision", nullable: true),
                    set_warn_weight = table.Column<double>(type: "double precision", nullable: true),
                    set_alarm_net_weight = table.Column<double>(type: "double precision", nullable: true),
                    hart_en = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_operational_parameters", x => x.id);
                    table.ForeignKey(
                        name: "FK_operational_parameters_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "site_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    site_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    site_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    site_location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    site_description = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    port = table.Column<int>(type: "integer", nullable: true),
                    protocol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    internal_camera_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    internal_camera_username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    internal_camera_password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    global_camera_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    global_camera_username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    global_camera_password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    opcua_endpoint = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    opcua_security_policy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    opcua_security_mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    opcua_anonymous = table.Column<bool>(type: "boolean", nullable: false),
                    opcua_username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    opcua_password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    opcua_session_timeout = table.Column<int>(type: "integer", nullable: false),
                    opcua_request_timeout = table.Column<int>(type: "integer", nullable: false),
                    contact_person = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    operating_pressure_min = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    operating_pressure_max = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    pump_count = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_online = table.Column<bool>(type: "boolean", nullable: false),
                    connection_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_heartbeat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    alarm_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    alarm_phone_numbers = table.Column<string>(type: "text", nullable: true),
                    alarm_email_addresses = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_site_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_site_configs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "system_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    phone_alarm_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    phone_access_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone_access_key = table.Column<string>(type: "text", nullable: true),
                    sms_access_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sms_access_key = table.Column<string>(type: "text", nullable: true),
                    smtp_server = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    smtp_port = table.Column<int>(type: "integer", nullable: false),
                    email_account = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    email_password = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_system_configs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_alarm_notifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    alarm_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_alarm_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sms_alarm_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    email_alarm_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    global_photo_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    internal_photo_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    scope = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    scope_target = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_alarm_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_alarm_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    temp_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    temp_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    humidity_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    humidity_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    level_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    level_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    level_diff_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    level_diff_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    water_temp_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    water_temp_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    motor_speed_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    motor_speed_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    motor_current_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    motor_current_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    motor_power_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    motor_power_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    motor_torque_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    motor_torque_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    motor_winding_temp_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    motor_winding_temp_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    brush_speed_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    brush_speed_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    brush_current_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    brush_current_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    brush_temp_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    brush_temp_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    brush_power_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    brush_power_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    scale_weight_high_threshold = table.Column<double>(type: "double precision", nullable: false),
                    scale_weight_low_threshold = table.Column<double>(type: "double precision", nullable: false),
                    default_system_mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    auto_error_reset = table.Column<bool>(type: "boolean", nullable: false),
                    error_check_interval = table.Column<int>(type: "integer", nullable: false),
                    alarm_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    alarm_sound_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    alarm_email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    alarm_sms_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    data_refresh_interval = table.Column<int>(type: "integer", nullable: false),
                    chart_data_points = table.Column<int>(type: "integer", nullable: false),
                    ui_theme = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ui_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    show_advanced_controls = table.Column<bool>(type: "boolean", nullable: false),
                    custom_settings = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "alarm_configs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: true),
                    alarm_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    alarm_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    alarm_message = table.Column<string>(type: "text", nullable: false),
                    alarm_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    trigger_variable = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    trigger_bit = table.Column<int>(type: "integer", nullable: true),
                    auto_clear = table.Column<bool>(type: "boolean", nullable: false),
                    require_confirmation = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    solution_guide = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alarm_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_alarm_configs_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "alarm_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    alarm_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    alarm_description = table.Column<string>(type: "text", nullable: true),
                    node_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    node_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    severity = table.Column<AlarmSeverity>(type: "alarmseverity", nullable: false),
                    status = table.Column<AlarmStatus>(type: "alarmstatus", nullable: false),
                    current_value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    alarm_value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    alarm_start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    alarm_end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acknowledged_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acknowledged_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alarm_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_alarm_records_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "currents",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    current = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_quality = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currents", x => x.id);
                    table.ForeignKey(
                        name: "FK_currents_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "downstream_water_levels",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    water_level = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data_quality = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_downstream_water_levels", x => x.id);
                    table.ForeignKey(
                        name: "FK_downstream_water_levels_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "externalhumiditys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    externalhumidity = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_quality = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_externalhumiditys", x => x.id);
                    table.ForeignKey(
                        name: "FK_externalhumiditys_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "externaltemps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    externaltemp = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_quality = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_externaltemps", x => x.id);
                    table.ForeignKey(
                        name: "FK_externaltemps_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "flow_velocities",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    velocity = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data_quality = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_flow_velocities", x => x.id);
                    table.ForeignKey(
                        name: "FK_flow_velocities_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "instantaneous_flows",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flow_rate = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data_quality = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instantaneous_flows", x => x.id);
                    table.ForeignKey(
                        name: "FK_instantaneous_flows_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "internalhumiditys",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    internalhumidity = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    data_quality = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internalhumiditys", x => x.id);
                    table.ForeignKey(
                        name: "FK_internalhumiditys_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "internaltemps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    internaltemp = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_quality = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_internaltemps", x => x.id);
                    table.ForeignKey(
                        name: "FK_internaltemps_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "motorwindingtemps",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    motorwindingtemp = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_quality = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motorwindingtemps", x => x.id);
                    table.ForeignKey(
                        name: "FK_motorwindingtemps_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "netweights",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    netweight = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    data_quality = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_netweights", x => x.id);
                    table.ForeignKey(
                        name: "FK_netweights_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "speeds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    speed = table.Column<decimal>(type: "numeric", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    data_quality = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_speeds", x => x.id);
                    table.ForeignKey(
                        name: "FK_speeds_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "upstream_water_levels",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    water_level = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    data_quality = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_upstream_water_levels", x => x.id);
                    table.ForeignKey(
                        name: "FK_upstream_water_levels_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_sites",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    is_owner = table.Column<bool>(type: "boolean", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sites", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_sites_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_sites_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "water_temperatures",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    site_id = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    temperature = table.Column<decimal>(type: "numeric(10,3)", nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    data_quality = table.Column<short>(type: "smallint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_water_temperatures", x => x.id);
                    table.ForeignKey(
                        name: "FK_water_temperatures_site_configs_site_id",
                        column: x => x.site_id,
                        principalTable: "site_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alarm_configs_alarm_category",
                table: "alarm_configs",
                column: "alarm_category");

            migrationBuilder.CreateIndex(
                name: "IX_alarm_configs_alarm_code",
                table: "alarm_configs",
                column: "alarm_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alarm_configs_is_active",
                table: "alarm_configs",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_alarm_configs_severity",
                table: "alarm_configs",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "IX_alarm_configs_site_id",
                table: "alarm_configs",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_alarm_records_site_id",
                table: "alarm_records",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "idx_currents_site_time",
                table: "currents",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_downstream_levels_site_time",
                table: "downstream_water_levels",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_externalhumiditys_site_time",
                table: "externalhumiditys",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_externaltemps_site_time",
                table: "externaltemps",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_velocities_site_time",
                table: "flow_velocities",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_flows_site_time",
                table: "instantaneous_flows",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_internalhumiditys_site_time",
                table: "internalhumiditys",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_internaltemps_site_time",
                table: "internaltemps",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_motorwindingtemps_site_time",
                table: "motorwindingtemps",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_netweights_site_time",
                table: "netweights",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_operational_parameters_user_id",
                table: "operational_parameters",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_site_configs_site_code",
                table: "site_configs",
                column: "site_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_site_configs_user_id",
                table: "site_configs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_speeds_site_time",
                table: "speeds",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_configs_user_id",
                table: "system_configs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_telemetry_minute_site_time",
                table: "telemetry_minute",
                columns: new[] { "site_code", "ts_minute" });

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_minute_SiteCode",
                table: "telemetry_minute",
                column: "site_code");

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_minute_TsMinute",
                table: "telemetry_minute",
                column: "ts_minute");

            migrationBuilder.CreateIndex(
                name: "uq_site_minute",
                table: "telemetry_minute",
                columns: new[] { "site_code", "ts_minute" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_upstream_levels_site_time",
                table: "upstream_water_levels",
                columns: new[] { "site_id", "timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_alarm_notifications_user_id",
                table: "user_alarm_notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_user_id",
                table: "user_settings",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_sites_site_id",
                table: "user_sites",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "uq_user_site",
                table: "user_sites",
                columns: new[] { "user_id", "site_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_temps_site_time",
                table: "water_temperatures",
                columns: new[] { "site_id", "timestamp" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alarm_configs");

            migrationBuilder.DropTable(
                name: "alarm_records");

            migrationBuilder.DropTable(
                name: "currents");

            migrationBuilder.DropTable(
                name: "downstream_water_levels");

            migrationBuilder.DropTable(
                name: "externalhumiditys");

            migrationBuilder.DropTable(
                name: "externaltemps");

            migrationBuilder.DropTable(
                name: "flow_velocities");

            migrationBuilder.DropTable(
                name: "instantaneous_flows");

            migrationBuilder.DropTable(
                name: "internalhumiditys");

            migrationBuilder.DropTable(
                name: "internaltemps");

            migrationBuilder.DropTable(
                name: "login_logs");

            migrationBuilder.DropTable(
                name: "motorwindingtemps");

            migrationBuilder.DropTable(
                name: "netweights");

            migrationBuilder.DropTable(
                name: "operational_parameters");

            migrationBuilder.DropTable(
                name: "speeds");

            migrationBuilder.DropTable(
                name: "system_configs");

            migrationBuilder.DropTable(
                name: "telemetry_minute");

            migrationBuilder.DropTable(
                name: "upstream_water_levels");

            migrationBuilder.DropTable(
                name: "user_alarm_notifications");

            migrationBuilder.DropTable(
                name: "user_settings");

            migrationBuilder.DropTable(
                name: "user_sites");

            migrationBuilder.DropTable(
                name: "water_temperatures");

            migrationBuilder.DropTable(
                name: "site_configs");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
