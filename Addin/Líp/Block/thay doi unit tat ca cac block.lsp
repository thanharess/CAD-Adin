(defun c:BLchangeallunitmm ( / doc blocks blk)
  (vl-load-com)

  (setq doc    (vla-get-ActiveDocument (vlax-get-acad-object))
        blocks (vla-get-Blocks doc)
  )

  (vlax-for blk blocks
    ;; bỏ block hệ thống (*Model_Space, *Paper_Space)
    (if (not (wcmatch (vla-get-Name blk) "`**"))
      (vla-put-Units blk 4) ; 4 = Millimeters
    )
  )

  (princ "\n✔ All block units changed to MILLIMETERS.")
  (princ)
)
