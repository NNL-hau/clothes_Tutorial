window.googleAuthFunctions = {
    initialize: function (dotNetHelper, clientId) {
        google.accounts.id.initialize({
            client_id: clientId,
            callback: (response) => {
                dotNetHelper.invokeMethodAsync('HandleGoogleLogin', response.credential);
            }
        });
        
        // Render the button specifically only if the container exists
        // Wait a slight bit to ensure DOM
        setTimeout(() => {
             const buttonContainer = document.getElementById("google-btn-container");
             if (buttonContainer) {
                 google.accounts.id.renderButton(
                     buttonContainer,
                     { theme: "outline", size: "large", width: "100%" }  // customization attributes
                 );
             }
        }, 100);
    }
};
