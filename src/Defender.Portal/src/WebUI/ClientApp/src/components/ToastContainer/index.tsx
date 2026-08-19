import { ToastContainer } from "react-toastify";


const AppToastContainer = () => {
  return <ToastContainer
    position="top-right"
    autoClose={5000}
    hideProgressBar={false}
    newestOnTop={false}
    closeOnClick
    rtl={false}
    pauseOnFocusLoss
    draggable
    pauseOnHover
    theme='dark'
    style={{ zIndex: 9999 }}
  />
};


export default AppToastContainer;
