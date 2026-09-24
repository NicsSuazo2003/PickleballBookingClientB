// Interfaces/IBookingService.cs
using PickleballBookingSystem.DTOs;

namespace PickleballBookingSystem.Interfaces;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, Guid clientId);
    Task<BookingDto> GetBookingAsync(Guid id, Guid clientId);
    Task<BookingDto> GetBookingByReferenceAsync(string referenceCode, Guid clientId);
    Task<BookingDto> TrackBookingAsync(string referenceCode, string? email, Guid clientId);
    Task<List<BookingDto>> GetBookingsAsync(Guid clientId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<BookingDto>> GetAllBookingsAsync(Guid clientId);
    Task<BookingDto> UpdateBookingStatusAsync(Guid id, string status, Guid clientId);
    Task<BookingDto> AdminUpdateBookingAsync(Guid id, AdminUpdateBookingRequest request, Guid clientId);
    Task<BookingDto> UploadPaymentAsync(Guid id, string screenshotBase64, string? paymentReference, Guid clientId);
    // ✅ FIXED: screenshotBase64 is now nullable — a payment can be submitted
    // with just a reference number and no screenshot.
    Task<BookingDto> UploadPaymentScreenshotAsync(Guid id, string? screenshotBase64, string? paymentReference, string? paymentMethod, Guid clientId);
    Task ConfirmPaymentAsync(Guid id, Guid clientId);
    Task CancelBookingAsync(Guid id, Guid clientId);
    Task AutoCompletePastBookingsAsync(Guid clientId);
    Task CancelExpiredPaymentsAsync(Guid clientId);
    Task<List<BookingSummaryDto>> TrackBookingSummariesByEmailAsync(string email, Guid clientId);

    Task<BookingDto> CreateStaffBookingAsync(StaffCreateBookingRequest request, Guid clientId);
}