﻿using DoanKhoaClient.Models;
using DoanKhoaClient.Services;
using DoanKhoaClient.Views;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DoanKhoaClient.Helpers;
namespace DoanKhoaClient.ViewModels
{
    public partial class LoginViewModel : INotifyPropertyChanged
    {
        #region Private Fields
        private readonly AuthService _authService;
        private string _username;
        private string _password;
        private string _errorMessage;
        private string _otpCode;
        private string _userId;
        private bool _isLoading;
        private bool _showOtpInput = false;
        private Visibility _usernamePlaceholderVisibility = Visibility.Visible;
        private Visibility _passwordPlaceholderVisibility = Visibility.Visible;
        private Visibility _otpPlaceholderVisibility = Visibility.Visible;
        private bool _rememberMe = false;
        #endregion

        #region Properties
        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                    UsernamePlaceholderVisibility = string.IsNullOrWhiteSpace(value)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    ValidateCanLogin();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                    ValidateCanLogin();
                }
            }
        }

        public string OtpCode
        {
            get => _otpCode;
            set
            {
                if (_otpCode != value)
                {
                    _otpCode = value;
                    OnPropertyChanged();
                    OtpPlaceholderVisibility = string.IsNullOrWhiteSpace(value)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    ValidateCanVerifyOtp();
                }
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                    (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (VerifyOtpCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public bool ShowOtpInput
        {
            get => _showOtpInput;
            set
            {
                if (_showOtpInput != value)
                {
                    _showOtpInput = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility UsernamePlaceholderVisibility
        {
            get => _usernamePlaceholderVisibility;
            set
            {
                if (_usernamePlaceholderVisibility != value)
                {
                    _usernamePlaceholderVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility PasswordPlaceholderVisibility
        {
            get => _passwordPlaceholderVisibility;
            set
            {
                if (_passwordPlaceholderVisibility != value)
                {
                    _passwordPlaceholderVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility OtpPlaceholderVisibility
        {
            get => _otpPlaceholderVisibility;
            set
            {
                if (_otpPlaceholderVisibility != value)
                {
                    _otpPlaceholderVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool RememberMe
        {
            get => _rememberMe;
            set
            {
                _rememberMe = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #region Commands
        public ICommand LoginCommand { get; private set; }
        public ICommand VerifyOtpCommand { get; private set; }
        public ICommand NavigateToRegisterCommand { get; private set; }
        public ICommand ForgotPasswordCommand { get; private set; }
        public ICommand PasswordChangedCommand { get; private set; }
        #endregion

        #region Constructor
        public LoginViewModel()
        {
            _authService = new AuthService();
            _showOtpInput = false; // Đảm bảo mặc định là false

            // Initialize commands
            LoginCommand = new RelayCommand(ExecuteLogin, CanExecuteLogin);
            VerifyOtpCommand = new RelayCommand(ExecuteVerifyOtp, CanVerifyOtp);
            NavigateToRegisterCommand = new RelayCommand(ExecuteNavigateToRegister);
            ForgotPasswordCommand = new RelayCommand(ExecuteForgotPassword);
            PasswordChangedCommand = new RelayCommand(ExecutePasswordChanged);

            // Kiểm tra session tồn tại
            CheckExistingSession();

            // Kiểm tra remember credentials
            LoadRememberCredentials();
        }
        #endregion

        #region Command Methods

        private bool CanExecuteLogin(object parameter)
        {
            return !IsLoading &&
                  !string.IsNullOrWhiteSpace(Username) &&
                  !string.IsNullOrWhiteSpace(Password);
        }

        private bool CanVerifyOtp(object parameter)
        {
            return !IsLoading &&
                  !string.IsNullOrWhiteSpace(OtpCode);
        }

        private void ValidateCanLogin()
        {
            (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void ValidateCanVerifyOtp()
        {
            (VerifyOtpCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void CheckExistingSession()
        {
            try
            {
                var session = SessionService.GetSession();
                if (session != null)
                {
                    System.Diagnostics.Debug.WriteLine("Valid session found, auto-login...");

                    // Tự động đăng nhập với session
                    var user = new User
                    {
                        Id = session.UserId,
                        Username = session.Username,
                        DisplayName = session.DisplayName,
                        Email = session.Email,
                        Role = session.Role,
                        AvatarUrl = session.AvatarUrl
                    };

                    // Lưu user vào Application Properties
                    App.Current.Properties["CurrentUser"] = user;
                    AccessControl.SetCurrentUser(new AuthResponse
                    {
                        Id = user.Id,
                        Username = user.Username,
                        DisplayName = user.DisplayName,
                        Email = user.Email,
                        Role = user.Role,
                        AvatarUrl = user.AvatarUrl
                    });

                    // Chuyển đến trang chính
                    NavigateToMainPage(user);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking existing session: {ex.Message}");
            }
        }

        private void LoadRememberCredentials()
        {
            try
            {
                var rememberData = SessionService.GetRememberCredentials();
                if (rememberData != null)
                {
                    Username = rememberData.Username;
                    Password = rememberData.Password;
                    RememberMe = true;
                    System.Diagnostics.Debug.WriteLine($"Loaded remember credentials for: {Username}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading remember credentials: {ex.Message}");
            }
        }

        private async void ExecuteLogin(object parameter)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var request = new LoginRequest
                {
                    Username = Username,
                    Password = Password
                };

                var response = await _authService.LoginAsync(request);

                if (response.RequiresTwoFactor)
                {
                    // Xử lý xác thực hai lớp như hiện tại
                    _userId = response.Id;
                    ShowOtpInput = true;
                    MessageBox.Show(response.Message, "Xác thực hai bước", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (!string.IsNullOrEmpty(response.Id))
                {
                    // Lưu thông tin người dùng vào Application.Current.Properties
                    var currentUser = new User
                    {
                        Id = response.Id,
                        Username = response.Username,
                        DisplayName = response.DisplayName,
                        Email = response.Email,
                        AvatarUrl = response.AvatarUrl,
                        Role = response.Role
                    };

                    // Lưu session
                    SessionService.SaveSession(currentUser);

                    // Lưu remember credentials nếu được chọn
                    if (RememberMe)
                    {
                        SessionService.SaveRememberCredentials(Username, Password);
                    }
                    else
                    {
                        SessionService.DeleteRememberCredentials();
                    }

                    App.Current.Properties["CurrentUser"] = currentUser;
                    AccessControl.SetCurrentUser(response);

                    // Hiển thị thông báo chào mừng kết hợp với role
                    MessageBox.Show($"Chào mừng {response.Role} {response.DisplayName ?? response.Username}!", "Đăng nhập thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Mở trang cho người dùng thông thường
                    NavigateToMainPage(currentUser);
                }
                else
                {
                    // Hiển thị thông báo lỗi
                    ErrorMessage = response.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi đăng nhập: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void ExecuteVerifyOtp(object parameter)
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var request = new VerifyOtpRequest
                {
                    UserId = _userId,
                    Otp = OtpCode
                };

                var response = await _authService.VerifyOtpAsync(request);

                if (!string.IsNullOrEmpty(response.Id))
                {
                    // Đăng nhập thành công, lưu thông tin user và chuyển đến màn hình chính
                    App.Current.Properties["CurrentUser"] = new User
                    {
                        Id = response.Id,
                        Username = response.Username,
                        DisplayName = response.DisplayName,
                        Email = response.Email,
                        AvatarUrl = response.AvatarUrl,
                    };

                    MessageBox.Show($"Chào mừng, {response.DisplayName}!", "Đăng nhập thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Chuyển đến màn hình chính
                    var chatWindow = new UserChatView();
                    chatWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    chatWindow.Show();

                    // Đóng cửa sổ đăng nhập hiện tại
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is LoginView)
                        {
                            window.Close();
                            break;
                        }
                    }
                }
                else
                {
                    // Hiển thị thông báo lỗi
                    ErrorMessage = response.Message;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xác thực OTP: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteNavigateToRegister(object parameter)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Đang chuyển đến trang đăng ký...");

                // Tạo window mới
                var registerWindow = new RegisterView();
                registerWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;

                // Hiển thị cửa sổ mới
                registerWindow.Show();

                // Tìm cửa sổ hiện tại
                Window currentWindow = null;
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is LoginView)
                    {
                        currentWindow = window;
                        break;
                    }
                }

                // Đóng cửa sổ đăng nhập hiện tại
                if (currentWindow != null)
                {
                    currentWindow.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Lỗi khi chuyển trang: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteForgotPassword(object parameter)
        {
            MessageBox.Show("Tính năng quên mật khẩu sẽ được triển khai trong phiên bản sau.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecutePasswordChanged(object parameter)
        {
            if (parameter is PasswordBox passwordBox)
            {
                Password = passwordBox.Password;
                PasswordPlaceholderVisibility = string.IsNullOrWhiteSpace(passwordBox.Password)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void NavigateToMainPage(User user)
        {
            try
            {
                var userDashboard = new HomePageView();
                userDashboard.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                userDashboard.Show();

                // Đóng cửa sổ đăng nhập hiện tại
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is LoginView)
                    {
                        window.Close();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to main page: {ex.Message}");
            }
        }
        #endregion

        #region INotifyPropertyChanged Implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region RelayCommand Implementation
        public class RelayCommand : ICommand
        {
            private readonly Action<object> _execute;
            private readonly Predicate<object> _canExecute;

            public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object parameter)
            {
                return _canExecute == null || _canExecute(parameter);
            }

            public void Execute(object parameter)
            {
                _execute(parameter);
            }

            public event EventHandler CanExecuteChanged;

            public void RaiseCanExecuteChanged()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        #endregion
    }
}