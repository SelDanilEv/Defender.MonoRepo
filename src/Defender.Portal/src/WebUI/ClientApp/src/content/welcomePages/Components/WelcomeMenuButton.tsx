import useUtils from "src/appUtils";
import LockedButton from "src/components/LockedComponents/LockedButton/LockedButton";

const WelcomeMenuButton = (props: any) => {
  const u = useUtils();
  const isCompact = !!props.compact;

  return (
    <LockedButton
      variant="contained"
      size="small"
      sx={{
        fontSize: isCompact ? "0.75rem" : "0.8em",
        minHeight: isCompact ? 28 : undefined,
        px: isCompact ? 1.5 : undefined,
        py: isCompact ? 0.25 : undefined,
        lineHeight: 1.2,
      }}
      onClick={() => {
        if (props.onClick) {
          props.onClick();
        }
        return u.react.navigate(props.path);
      }}
    >
      {props.text}
    </LockedButton>
  );
};

export default WelcomeMenuButton;
