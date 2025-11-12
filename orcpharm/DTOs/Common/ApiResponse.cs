using System.Collections.Generic;

namespace DTOs.Common;

/// <summary>
/// DTO genérico para respostas de API
/// </summary>
/// <typeparam name="T">Tipo de dados da resposta</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensagem da resposta
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Dados da resposta
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Lista de erros (se houver)
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Construtor para resposta bem-sucedida
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message ?? "Operação realizada com sucesso",
            Data = data
        };
    }

    /// <summary>
    /// Construtor para resposta de erro
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    /// <summary>
    /// Construtor para resposta de erro com erro único
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, string error)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { error }
        };
    }
}

/// <summary>
/// DTO para resposta de API sem dados (apenas sucesso/erro)
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indica se a operação foi bem-sucedida
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensagem da resposta
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Lista de erros (se houver)
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Construtor para resposta bem-sucedida
    /// </summary>
    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message ?? "Operação realizada com sucesso"
        };
    }

    /// <summary>
    /// Construtor para resposta de erro
    /// </summary>
    public static ApiResponse ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}
