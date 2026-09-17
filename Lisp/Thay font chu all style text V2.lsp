;;; =====================================================
;;; FORCE TIMES NEW ROMAN
;;; AutoCAD Mechanical 2020
;;; =====================================================

(defun c:FONTCHANGEALLTIMENEWROMAN (/ acad doc styles style fontObj count)

  (vl-load-com)

  (setq acad   (vlax-get-acad-object))
  (setq doc    (vla-get-ActiveDocument acad))
  (setq styles (vla-get-TextStyles doc))

  (setq count 0)

  (princ "\nĐang ép font Times New Roman...")

  (vlax-for style styles

    (if
      (not
        (vl-catch-all-error-p

          (vl-catch-all-apply
            'vla-SetFont
            (list style "Times New Roman" :vlax-false :vlax-false 0 34)
          )
        )
      )

      (setq count (1+ count))
    )
  )

  (vla-Regen doc acAllViewports)

  (princ
    (strcat
      "\n✓ Hoàn thành: "
      (itoa count)
      " style."
    )
  )

  (princ)
)