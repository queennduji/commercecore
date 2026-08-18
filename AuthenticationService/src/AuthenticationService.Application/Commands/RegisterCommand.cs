using AuthenticationService.Application.Common;
using AuthenticationService.Application.Dtos;
using MediatR;

namespace AuthenticationService.Application.Commands;

/// <summary>PhoneNumber is optional and, if provided, must be E.164 format (e.g. +15551234567) —
/// that's what Twilio expects and what NotificationService's SMS channel is keyed off of.</summary>
public record RegisterCommand(string Email, string Password, string? PhoneNumber = null) : IRequest<ServiceResult<AuthTokens>>;
